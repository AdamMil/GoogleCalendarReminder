using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            public override string ToString() => Event.Summary + " at " + (Event.Start.DateTimeRaw ?? Event.Start.Date);
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
                            defaultMinutes[i] = calendar.DefaultReminders == null ? -1 :
                                calendar.DefaultReminders.Where(r => r.Method == "popup")
                                    .Aggregate(-1, (min, e) => Math.Max(min, e.Minutes ?? -1));
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
                                bool deleted = e.Start == null, allDay = !deleted && !string.IsNullOrEmpty(e.Start.Date);
                                if(!deleted)
                                {
                                    const DateTimeStyles Style = DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AssumeLocal;
                                    if(!e.Start.DateTime.HasValue) // if it's an all-day event, ensure the DateTime field is set
                                    {
                                        e.Start.DateTime = DateTime.ParseExact(e.Start.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, Style);
                                    }
                                    if(e.End != null && !string.IsNullOrEmpty(e.End.Date) && !e.End.DateTime.HasValue) // ditto for the End
                                    {
                                        e.End.DateTime = DateTime.ParseExact(e.End.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, Style);
                                    }
                                }

                                bool ended = (e.End?.DateTime).HasValue && utcNow > e.End.DateTime.Value.ToUniversalTime();
                                if(ended || deleted && listRequest.SyncToken == null) continue; // report deleted items if incremental
                                int reminderMinutes = defaultMinutes[i];
                                if(e.Reminders?.UseDefault == false) // if the event has a reminder override...
                                {
                                    reminderMinutes = e.Reminders.Overrides == null ? -1 : // use it
                                        e.Reminders.Overrides.Where(o => o.Method == "popup").Aggregate(
                                            -1, (min, o) => Math.Max(min, o.Minutes ?? reminderMinutes));
                                    // we can't distinguish between all-day events in calendars with no default all-day event reminders
                                    // and all-day events that override and remove the calendars' default all-day event reminder because
                                    // in both cases they're represented as UseDefault == false and Overrides null/empty
                                    if(reminderMinutes < 0 && allDay && defaultReminder >= 0) // so assume no calendar-level default
                                    {
                                        reminderMinutes = defaultReminder != 0 ? defaultReminder : 1440; // and use our own (1 day)
                                    }
                                }
                                else if(reminderMinutes < 0 && defaultReminder >= 0) // otherwise if we should use our default...
                                {
                                    reminderMinutes = defaultReminder != 0 ? defaultReminder : allDay ? 1440 : 30; // do so
                                }

                                DateTime alarmAt = deleted || reminderMinutes < 0 ?
                                    default : e.Start.DateTime.Value.ToUniversalTime().AddMinutes(-reminderMinutes);
                                var alarm = new EventAlarm(alarmAt, e);
                                if(!fullSync && eventMap.TryGetValue(e.Id, out int index)) // if we already have this event...
                                {
                                    events[index] = alarm; // update it
                                }
                                else if(alarmAt != default) // otherwise, only add the event if it has a reminder
                                {
                                    eventMap[e.Id] = events.Count;
                                    events.Add(alarm);
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
                    catch(Exception ex) when (HasException<System.Net.Sockets.SocketException>(ex) || ex is HttpRequestException)
                    {
                        Disconnected?.Invoke(this);
                        failed |= true;
                    }
                    catch(Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Error?.Invoke(this, $"An unknown error occurred. {ex.GetType().Name} - {ex.Message}");
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
                    Utils.Hash(sha, a.AlarmAt);
                    Utils.Hash(sha, a.Event.Id);
                    Utils.Hash(sha, a.Event.Start != null ? a.Event.Start.DateTimeRaw ?? a.Event.Start.Date : null);
                    Utils.Hash(sha, a.Event.End   != null ? a.Event.End.DateTimeRaw   ?? a.Event.End.Date   : null);
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