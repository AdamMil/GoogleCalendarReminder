﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace CalendarReminder
{
    sealed class EventTracker : IDisposable
    {
        public EventTracker(CalendarService service, string[] calendarIds)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
            this.calendarIds = calendarIds ?? throw new ArgumentNullException(nameof(calendarIds));
            timer = new Timer(OnTimer);
            Program.Resume += OnResume;
        }

        public struct EventAlarm
        {
            public EventAlarm(DateTime at, Event e) => (AlarmAt, Event) = (at, e);
            public override string ToString() => Event.Summary + " at " + Event.Start.DateTimeRaw;
            public readonly DateTime AlarmAt;
            public readonly Event Event;
        }

        public event Action<EventTracker> Alarm;
        public event Action<EventTracker> Connected;
        public event Action<EventTracker> Disconnected;
        public event Action<EventTracker, string> Error;
        public event Action<EventTracker> EventsUpdated;

        public CalendarService Service { get; private set; }

        public void Dispose()
        {
            Program.Resume -= OnResume;
            timer.Dispose();
        }

        public void OnConfigChanged() => DoFullSync();

        public async Task Run(CancellationToken cancelToken)
        {
            int[] defaultMinutes = new int[calendarIds.Length];
            var events = new List<EventAlarm>(64);
            var eventMap = new Dictionary<string, int>(64);
            var lastMetaLoad = new DateTime[calendarIds.Length]; // when we last reloaded the metadata for each calendar
            var syncIds = new string[calendarIds.Length];
            string lastHash = null;
            while(!cancelToken.IsCancellationRequested)
            {
                DateTime utcNow = DateTime.UtcNow;
                int defaultReminder = Program.DataStore.Get<int>(Settings.DefaultReminder);
                bool fullSync, failed = false;
                lock(syncLock) fullSync = (utcNow - lastFullSync).Ticks >= TimeSpan.TicksPerHour;
                bool maybeChanged = fullSync; // if we're doing a full sync, we can't know that nothing changed since last time
                if(fullSync)
                {
                    events.Clear();
                    eventMap.Clear();
                    lastFullSync = utcNow;
                }

                for(int i = 0; i < calendarIds.Length; i++)
                {
                    retry:
                    try
                    {
                        if((utcNow - lastMetaLoad[i]).Ticks >= TimeSpan.TicksPerMinute*5)
                        {
                            CalendarListEntry calendar = await Service.CalendarList.Get(calendarIds[i]).ExecuteAsync(cancelToken).ConfigureAwait(false);
                            defaultMinutes[i] = calendar.DefaultReminders.Aggregate(int.MaxValue, (min, e) => Math.Min(min, e.Minutes ?? int.MaxValue));
                            lastMetaLoad[i] = utcNow;
                        }

                        var listRequest = Service.Events.List(calendarIds[i]);
                        listRequest.SingleEvents = true; // expand recurring events
                        string syncId;
                        lock(syncIds) syncId = syncIds[i];
                        if(fullSync || syncId == null)
                        {
                            listRequest.TimeMin = utcNow.AddMinutes(-1); // go back one minute in case of clock desync
                            listRequest.TimeMax = utcNow.AddDays(30); // look up to 30 days ahead
                        }
                        else
                        {
                            listRequest.SyncToken = syncId;
                        }

                        while(true) // for each page
                        {
                            var page = await listRequest.ExecuteAsync(cancelToken).ConfigureAwait(false);
                            if(page.Items.Count != 0) maybeChanged = true;
                            foreach(Event e in page.Items)
                            {
                                bool deleted = e.Start?.DateTime == null;
                                bool ended = (e.End?.DateTime).HasValue && utcNow > e.End.DateTime.Value.ToUniversalTime();
                                if(ended || deleted && listRequest.SyncToken == null) continue; // report deleted items if incremental
                                int reminderMinutes = defaultMinutes[i];
                                if(e.Reminders?.UseDefault == false)
                                {
                                    reminderMinutes = e.Reminders.Overrides == null ? int.MaxValue :
                                        e.Reminders.Overrides.Aggregate(
                                            int.MaxValue, (min, o) => Math.Min(min, o.Minutes ?? defaultMinutes[i]));
                                }
                                if(reminderMinutes == int.MaxValue && defaultReminder > 0) reminderMinutes = defaultReminder; // use default
                                // in an incremental sync, we may receive updates for events that already ended (but were edited)
                                if(deleted || reminderMinutes < int.MaxValue)
                                {
                                    var alarm = new EventAlarm(
                                        deleted ? default : e.Start.DateTime.Value.ToUniversalTime().AddMinutes(-reminderMinutes), e);
                                    if(fullSync || !eventMap.TryGetValue(e.Id, out int index)) // if we don't have this event already...
                                    {
                                        eventMap[e.Id] = events.Count;
                                        events.Add(alarm);
                                    }
                                    else // otherwise, we already have the event, so just update it
                                    {
                                        events[index] = alarm;
                                    }
                                }
                            }
                            if(page.NextPageToken == null)
                            {
                                syncIds[i] = page.NextSyncToken;
                                break;
                            }
                            listRequest.PageToken = page.NextPageToken;
                        }
                    }
                    catch(Google.GoogleApiException ex)
                    {
                        if(ex.HttpStatusCode == HttpStatusCode.Gone && syncIds[i] != null) { syncIds[i] = null; goto retry; }
                        if(ex.HttpStatusCode == HttpStatusCode.NotFound)
                        {
                            Error?.Invoke(this, $"Data not found for calendar {calendarIds[i]}. Check settings.");
                        }
                        failed |= true;
                    }
                    catch(Exception ex) when (HasException<System.Net.Sockets.SocketException>(ex))
                    {
                        Disconnected?.Invoke(this);
                        failed |= true;
                    }
                }

                if(!failed) Connected?.Invoke(this);
                if(maybeChanged) // if something may have changed...
                {
                    string hash = HashEvents(events); // we only want to trigger the alarm if there's something really new
                    if(hash == lastHash) maybeChanged = false; // if the hashes matched, then nothing changed
                    else lastHash = hash;
                }

                bool triggeringAlarm;
                lock(eventLock)
                {
                    calendarEvents = events;
                    triggeringAlarm = SetAlarmTimer(maybeChanged);
                }
                if(maybeChanged && !triggeringAlarm) EventsUpdated?.Invoke(this);

                await Task.Delay(TimeSpan.FromMinutes(1), cancelToken).ConfigureAwait(false);
            }
        }

        public List<EventAlarm> GetUpcomingEvents()
        {
            lock(eventLock) return new List<EventAlarm>(calendarEvents);
        }

        void DoFullSync()
        {
            lock(syncLock) lastFullSync = default;
        }

        void OnResume()
        {
            DoFullSync(); // after resuming from a sleep, make the next sync a full sync
            SetAlarmTimer(false); // and make sure the alarm timer is set correctly
        }

        void OnTimer(object _) => Alarm?.Invoke(this);

        bool SetAlarmTimer(bool eventsChanged)
        {
            lock(eventLock)
            {
                if(calendarEvents.Count == 0)
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else if(eventsChanged)
                {
                    TimeSpan toFirst = calendarEvents.Min(a => a.AlarmAt) - DateTime.UtcNow;
                    timer.Change((int)Math.Max(0, Math.Min(int.MaxValue, (long)toFirst.TotalMilliseconds)), Timeout.Infinite);
                    if(toFirst <= TimeSpan.Zero) return true;
                }
            }
            return false;
        }

        readonly string[] calendarIds;
        readonly Timer timer;
        readonly object eventLock = new object(), syncLock = new object();
        List<EventAlarm> calendarEvents = new List<EventAlarm>();
        DateTime lastFullSync;

        static bool HasException<T>(Exception ex) where T : Exception
        {
            for(; ex != null; ex = ex.InnerException)
            {
                if(ex is T) return true;
            }
            return false;
        }

        static string HashEvents(List<EventAlarm> events)
        {
            using(var sha = SHA1.Create())
            {
                foreach(EventAlarm a in events.OrderBy(a => a.Event.Id, StringComparer.Ordinal))
                {
                    Utils.Hash(sha, a.Event.Id);
                    Utils.Hash(sha, a.Event.Start?.DateTimeRaw);
                    Utils.Hash(sha, a.Event.End?.DateTimeRaw);
                    Utils.Hash(sha, a.Event.Summary);
                    Utils.Hash(sha, a.Event.HtmlLink);
                    Utils.Hash(sha, a.Event.Location);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return Convert.ToBase64String(sha.Hash);
            }
        }
    }
}