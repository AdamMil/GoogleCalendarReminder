using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace CalendarReminder
{
    partial class ReminderForm : FormBase
    {
        public ReminderForm()
        {
            InitializeComponent();
            snoozeTimer = new System.Threading.Timer(OnSnoozed);
            Icon = appIcon;
            SetDisconnected();
            foreach(int mins in reminderTimes) cmbTimes.Items.Add(GetSnoozeDescription(mins));
            Program.Resume += OnResume;

            var attr = (AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyFileVersionAttribute)).First();
            Text = $"{Text} v{attr.Version.Substring(0, attr.Version.LastIndexOf('.'))}";
        }

        public void ActivateAndShow(bool userShow, bool noSound = false)
        {
            // if the window is invisible, update the suggested snooze time and disable the form for a moment
            if(!Visible || WindowState == FormWindowState.Minimized)
            {
                SuggestSnoozeTime();
                if(!userShow)
                {
                    int delayMs = Program.DataStore.Get<int>(Settings.DisableTime);
                    if(delayMs >= 0)
                    {
                        enableTimer.Interval = delayMs != 0 ? delayMs : Settings.DefaultDisableMs;
                        Enabled = false;
                        enableTimer.Enabled = true;
                    }
                }
            }

            if(Visible) Activate();
            else Show();
            if(WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;

            cmbTimes.Focus(); // set the input focus to the snooze time box
            if(!startup && !userShow && !noSound && (uint)Environment.TickCount - lastPlay >= 60000 && // if we should play a sound...
               Program.DataStore.Get<string>(Settings.PlaySound) != null)
            {
                if(soundPlayer == null)
                {
                    string soundPath = Program.DataStore.Get<string>(Settings.PlaySound);
                    if(!string.IsNullOrEmpty(soundPath))
                    {
                        try
                        {
                            using(var stream = File.OpenRead(soundPath))
                            {
                                soundPlayer = new System.Media.SoundPlayer(stream);
                                soundPlayer.Load();
                            }
                        }
                        catch // if we failed to load the custom sound...
                        {
                            soundPath = string.Empty; // use the default sound
                        }
                    }
                    if(soundPath.Length == 0)
                    {
                        using(var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CalendarReminder.Resources.alarm.wav"))
                        {
                            soundPlayer = new System.Media.SoundPlayer(stream);
                            soundPlayer.Load();
                        }
                    }
                }
                soundPlayer.Play();
                lastPlay = (uint)Environment.TickCount; // don't play sounds too often
            }
        }

        public void SetTracker(EventTracker tracker)
        {
            cts.Cancel();
            this.tracker?.Dispose();
            SetDisconnected();

            cts = new CancellationTokenSource();
            this.tracker = tracker;
            tracker.Alarm += OnAlarm;
            tracker.Connected += _ => SetNotifyIcon(appIcon);
            tracker.Disconnected += _ => SetDisconnected(5);
            tracker.Error += (_, msg) => SetNotifyIcon(errorIcon, msg, 30);
            tracker.EventsUpdated += OnEventsUpdated;
            trackerTask = tracker.Run(cts.Token);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            HideForm();
            string[] calendarIds = Program.DataStore.Get<string[]>(Settings.CalendarIds);
            if(calendarIds == null || calendarIds.Length == 0) ShowSettingsForm();
            else BeginConnect(calendarIds);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            lstEvents.Columns[0].Width = lstEvents.Width - lstEvents.Columns[1].Width - 24;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshItems();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if(!quitting) // if the user is clicking the X...
            {
                e.Cancel = true; // don't actually close the form
                Enum.TryParse(Program.DataStore.Get<string>(Settings.OnClose), out OnCloseBehavior onClose);
                DialogResult dr;
                if(lstEvents.Items.Count == 0) // if there are no events...
                {
                    dr = DialogResult.None; // just hide the window
                }
                else if(onClose == OnCloseBehavior.Ask) // otherwise, if there's no default behavior...
                {
                    dr = MessageBox.Show(this, // ask
                        "Do you want to see more notifications about these events? (Yes = Snooze All, No = Dismiss All)",
                        "Snooze these events?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                }
                else // otherwise, use the default
                {
                    dr = onClose == OnCloseBehavior.DismissAll ? DialogResult.No : DialogResult.Yes;
                }

                if(dr != DialogResult.Cancel)
                {
                    if(dr == DialogResult.Yes) SnoozeAll();
                    else if(dr == DialogResult.No) DismissAll();
                    else HideForm(); // just hide the form
                }
            }
            else // otherwise, we're quitting, so cancel any background tasks and remove the notify icon before exiting
            {
                snoozeTimer.Dispose();
                cts.Cancel();
                notifyIcon.Visible = false;
            }
            base.OnFormClosing(e);
        }

        sealed class TrackedEvent
        {
            public TrackedEvent(Event e, DateTime alarmAt) { Event = e; AlarmAt = alarmAt; }
            public Event Event;
            public DateTime AlarmAt, SnoozeUntil;
        }

        bool IsEventSelected => lstEvents.SelectedIndices.Count != 0;

        async void BeginConnect(string[] calendarIds)
        {
            CalendarService service;
            try
            {
                service = await Program.ConnectAsync(Program.DataStore, cts.Token).ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                SetDisconnected(5);
                return;
            }
            catch(Exception ex)
            {
                SetNotifyIcon(errorIcon, $"Failed to connect. {ex.GetType().Name} - {ex.Message}", 30);
                return;
            }

            Invoke((Action)(() => SetTracker(new EventTracker(service, calendarIds))));

            // set 'startup' to false after 15 seconds so we'll start getting alarm sounds
            try
            {
                await Task.Delay(15000, cts.Token).ConfigureAwait(false);
                startup = false;
            }
            catch(OperationCanceledException) { }
        }

        void DismissAll()
        {
            foreach(ListViewItem item in lstEvents.Items) DismissEvent((Event)item.Tag);
            lstEvents.Items.Clear();
            OnItemCountChanged();
        }

        void DismissEvent(Event e)
        {
            dismissed[e.Id] = e.End.DateTime.Value.ToUniversalTime();
            trackedEvents.Remove(e.Id);
        }

        Event GetSelectedEvent() => IsEventSelected ? (Event)lstEvents.SelectedItems[0].Tag : null;

        void HideForm()
        {
            toolTip.Hide(lstEvents);
            Hide();
        }

        void OnAlarm(EventTracker tracker) => QueueInvoke(() => ReloadEvents(false, false, tracker));
        void OnEventsUpdated(EventTracker tracker) => QueueInvoke(() => ReloadEvents(false, true, tracker));

        void OnItemCountChanged()
        {
            miOpen.Enabled = lstEvents.Items.Count != 0;
            if(lstEvents.Items.Count == 0) // if there are no items left...
            {
                soundPlayer?.Stop(); // stop any sound that's playing
                HideForm(); // and hide the form
            }
        }

        void OnResume() => // on resumption from sleep or a session lock...
            QueueInvoke(() =>
            {
                lastPlay = ~(uint)Environment.TickCount; // play sounds again
                if(snoozeUntil != default) SetSnoozeTime(snoozeUntil, true); // if we were snoozing, correct the snooze timer
                SuggestSnoozeTime(); // and suggest a new snooze time for the selected item
            });

        void OnSnoozed(object _)
        {
            snoozeUntil = default; // OnAlarm will update snoozeUntil to the next snooze time
            OnAlarm(tracker);
        }

        void OpenBrowser(string link)
        {
            try { Process.Start(new ProcessStartInfo() { FileName = link, UseShellExecute = true }); }
            catch(Exception ex) { ShowError(ex, "Unable to open event URL"); }
        }

        void OpenSelectedItem()
        {
            string link = GetSelectedEvent()?.HtmlLink;
            if(!string.IsNullOrEmpty(link)) OpenBrowser(link);
        }

        void RefreshItems()
        {
            if(lstEvents.Items.Count != 0)
            {
                DateTime utcNow = DateTime.UtcNow; // update the time to all the events
                lstEvents.SuspendLayout();
                foreach(ListViewItem item in lstEvents.Items)
                {
                    item.SubItems[1].Text = GetTimeOffset(((Event)item.Tag).Start.DateTime.Value.ToUniversalTime() - utcNow);
                }
                lstEvents.ResumeLayout();
            }
        }

        void ReloadEvents(bool userTriggered, bool noSound = false, EventTracker tracker = null)
        {
            if(tracker == null) tracker = this.tracker;
            DateTime utcNow = DateTime.UtcNow, snoozeTil = default;

            // remove old dismissed events to prevent them from hanging around forever
            foreach(string key in dismissed.Where(p => (utcNow - p.Value).Ticks > TimeSpan.TicksPerHour).Select(p => p.Key).ToList())
            {
                dismissed.Remove(key);
            }

            // process events from the tracker
            foreach(EventTracker.EventAlarm a in tracker.GetUpcomingEvents())
            {
                if(!dismissed.ContainsKey(a.Event.Id))
                {
                    if(a.AlarmAt == default || a.AlarmAt > utcNow) // if it shouldn't be shown...
                    {
                        trackedEvents.Remove(a.Event.Id); // remove it from the list and reset any snooze
                    }
                    else if(trackedEvents.TryGetValue(a.Event.Id, out TrackedEvent te)) // otherwise, if we were already tracking the event...
                    {
                        te.AlarmAt = a.AlarmAt; // update our tracking information and find the earliest snooze time
                        te.Event = a.Event;
                        if(te.SnoozeUntil >= utcNow && (snoozeTil == default || te.SnoozeUntil < snoozeTil)) snoozeTil = te.SnoozeUntil;
                    }
                    else // otherwise, start tracking the event
                    {
                        trackedEvents[a.Event.Id] = new TrackedEvent(a.Event, a.AlarmAt);
                    }
                }
            }

            string selectedId = GetSelectedEvent()?.Id;
            reloadingList = true; // don't take certain actions on incidental selection change during list reload
            lstEvents.SuspendLayout();
            lstEvents.Items.Clear();
            string hash;
            using(var sha = System.Security.Cryptography.SHA1.Create())
            {
                foreach(TrackedEvent te in trackedEvents.Values
                    .Where(te => te.SnoozeUntil <= utcNow).OrderBy(te => te.Event.Start.DateTime.Value.ToUniversalTime()))
                {
                    ListViewItem item = lstEvents.Items.Add(!string.IsNullOrEmpty(te.Event.Summary) ? te.Event.Summary : "(No title)");
                    item.SubItems.Add(GetTimeOffset(te.Event.Start.DateTime.Value.ToUniversalTime() - utcNow));
                    item.Tag = te.Event;
                    if(te.Event.Id == selectedId) // reselect the previously selected item, if any
                    {
                        lstEvents.SelectedIndices.Add(lstEvents.Items.Count-1);
                        selectedId = null;
                    }

                    Utils.Hash(sha, te.Event.Id);
                    Utils.Hash(sha, te.Event.Start.DateTimeRaw ?? te.Event.Start.Date);
                    Utils.Hash(sha, te.AlarmAt);
                    Utils.Hash(sha, te.SnoozeUntil);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                hash = Convert.ToBase64String(sha.Hash);
            }
            lstEvents.ResumeLayout();
            reloadingList = false;

            if(lstEvents.Items.Count != 0 && lstEvents.SelectedIndices.Count == 0) lstEvents.SelectedIndices.Add(0);
            if(hash != lastEventHash) // only activate the form or play a sound if the items have substantially changed
            {
                if(lstEvents.Items.Count != 0) ActivateAndShow(userTriggered, noSound);
                OnItemCountChanged(); // OnItemCountChanged will hide the form if there are no items
                lastEventHash = hash;
            }
            startup = false; // we're not in the initial startup anymore

            SetSnoozeTime(snoozeTil, true);
        }

        void RemoveSelectedItemFromList()
        {
            int index = lstEvents.SelectedIndices[0];
            lstEvents.Items.RemoveAt(index);
            if(index == lstEvents.Items.Count) index--;
            if((uint)index < (uint)lstEvents.Items.Count) lstEvents.SelectedIndices.Add(index); // select the next one
            OnItemCountChanged(); // this will close the window if the list is empty
        }

        void SetDisconnected(int popupSecs = 0) => SetNotifyIcon(disconnectedIcon, "Not connected", popupSecs);

        void SetNotifyIcon(Icon icon, string msg = null, int popupSecs = 0)
        {
            string name = "Google Calendar Reminder", text = msg != null ? name + ": " + msg : name;
            notifyIcon.Icon = icon;
            notifyIcon.Text = text.Length < 64 ? text : msg.Length < 64 ? msg : msg.Substring(0, 60) + "...";
            if(popupSecs != 0)
            {
                notifyIcon.BalloonTipTitle = name;
                notifyIcon.BalloonTipText = msg;
                notifyIcon.ShowBalloonTip(popupSecs * 1000);
            }
        }

        void SetSnoozeTime(DateTime until, bool overwrite)
        {
            if(overwrite || snoozeUntil == default || until < snoozeUntil)
            {
                snoozeUntil = until;
                snoozeTimer.Change(until == default ? Timeout.Infinite : Math.Max(0, (int)(until - DateTime.UtcNow).TotalMilliseconds),
                    Timeout.Infinite);
            }
        }

        void ShowSettingsForm()
        {
            using(var form = new SettingsForm(tracker?.Service))
            {
                TopMost = false; // don't make the reminder form topmost when the settings form is open
                string[] oldIds = Program.DataStore.Get<string[]>(Settings.CalendarIds);
                if(form.ShowDialog() == DialogResult.OK)
                {
                    soundPlayer?.Dispose();
                    soundPlayer = null;
                    if(form.Service != tracker?.Service || // if we need a new tracker...
                        !oldIds.OrderBy(x => x).SequenceEqual(form.CalendarIds.OrderBy(x => x), StringComparer.Ordinal))
                    {
                        SetTracker(new EventTracker(form.Service, form.CalendarIds)); // create one
                    }
                    else // otherwise, tell the existing tracker that the configuration has changed
                    {
                        tracker.OnConfigChanged();
                    }
                }
                TopMost = true;
            }
        }

        void Snooze()
        {
            TimeSpan snoozeTime;
            try
            {
                snoozeTime = cmbTimes.SelectedIndex >= 0 ?
                    TimeSpan.FromMinutes(reminderTimes[cmbTimes.SelectedIndex]) : GetTimeOffset(cmbTimes.Text);
            }
            catch(FormatException)
            {
                ShowError($"Invalid snooze time: \"{cmbTimes.Text}\"", "Invalid snooze time");
                if(cmbTimes.Text == string.Empty) SuggestSnoozeTime();
                return;
            }

            Event ev = GetSelectedEvent();
            DateTime utcNow = DateTime.UtcNow;
            DateTime time = (snoozeTime.Ticks <= 0 ? ev.Start.DateTime.Value.ToUniversalTime() : utcNow) + snoozeTime;
            if(time > utcNow)
            {
                trackedEvents[ev.Id].SnoozeUntil = time;
                RemoveSelectedItemFromList();
                SetSnoozeTime(time, false);
            }
        }

        void SnoozeAll()
        {
            if(lstEvents.Items.Count != 0)
            {
                DateTime utcNow = DateTime.UtcNow, snoozeUntil = DateTime.MaxValue;
                foreach(ListViewItem item in lstEvents.Items)
                {
                    var e = (Event)item.Tag;
                    int mins = GetReminderMins(e);
                    DateTime time = (mins <= 0 ? e.Start.DateTime.Value.ToUniversalTime() : utcNow).AddMinutes(mins);
                    trackedEvents[e.Id].SnoozeUntil = time;
                    if(time < snoozeUntil) snoozeUntil = time;
                }
                SetSnoozeTime(snoozeUntil, false);
                lstEvents.Items.Clear();
                OnItemCountChanged();
            }
        }

        void SuggestSnoozeTime()
        {
            Event ev = GetSelectedEvent();
            if(ev != null)
            {
                int reminderMins = GetReminderMins(ev);
                cmbTimes.SelectedIndex = Array.IndexOf(reminderTimes, reminderMins);
                if(cmbTimes.SelectedIndex < 0) cmbTimes.Text = GetSnoozeDescription(reminderMins);
            }
        }

        void UserShowForm(bool unsnooze)
        {
            if(unsnooze)
            {
                foreach(TrackedEvent te in trackedEvents.Values) te.SnoozeUntil = default;
                snoozeUntil = default;
                ReloadEvents(true);
            }
            if(lstEvents.Items.Count != 0) ActivateAndShow(true);
        }

        void btnDismiss_Click(object sender, EventArgs e)
        {
            DismissEvent(GetSelectedEvent());
            RemoveSelectedItemFromList();
        }

        void btnDismissAll_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show(this, "Dismiss all events?", "Dismiss all?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                DismissAll();
            }
        }

        void btnSnooze_Click(object sender, EventArgs e) => Snooze();

        void enableTimer_Tick(object sender, EventArgs e)
        {
            enableTimer.Enabled = false;
            Enabled = true;
            cmbTimes.Focus();
        }

        void eventTimer_Tick(object sender, EventArgs e)
        {
            if(Visible) RefreshItems();
        }

        void itemContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Event ev = GetSelectedEvent();
            if(ev == null) // if there's no event...
            {
                e.Cancel = true; // don't show a menu
            }
            else // otherwise, there is an event
            {
                if(itemContextMenu.Items.Count > 1) // restore the menu to its original state
                {
                    itemContextMenu.Items.Clear();
                    itemContextMenu.Items.Add(miOpen);
                }
                // then add custom menu items  TODO: extract links from the description HTML?
                if(ev.ConferenceData?.EntryPoints != null)
                {
                    foreach(EntryPoint ep in ev.ConferenceData.EntryPoints)
                    {
                        if(string.IsNullOrEmpty(ep.Uri)) continue;
                        Image image;
                        string text, shortUri = ep.Uri;
                        if(Uri.TryCreate(shortUri, UriKind.Absolute, out Uri uri))
                        {
                            shortUri = !string.IsNullOrEmpty(uri.Host) ? uri.Host : uri.AbsolutePath;
                        }
                        if(ep.EntryPointType == "video") (image, text) = (videoImg, $"Join Video ({shortUri})");
                        else if(ep.EntryPointType == "phone") (image, text) = (phoneImg, $"Dial Phone ({shortUri})");
                        else if(ep.EntryPointType == "sip") (image, text) = (sipImg, $"Join SIP Conference ({shortUri})");
                        else if(ep.EntryPointType == "more") (image, text) = (conferenceImg, "Open Conference Info");
                        else (image, text) = (conferenceImg, $"Open Unknown Conferencing ({ep.EntryPointType} - {shortUri})");
                        itemContextMenu.Items.Add(text, image, (a, b) => OpenBrowser(ep.Uri));
                    }
                }
                if(IsUrl(ev.Location))
                {
                    string text = "Open Location Link";
                    if(Uri.TryCreate(ev.Location, UriKind.Absolute, out Uri uri)) text = $"{text} ({uri.Host})";
                    itemContextMenu.Items.Add(text, locationImg, (a, b) => OpenBrowser(ev.Location));
                }
            }
        }

        void lstEvents_DoubleClick(object sender, EventArgs e) => OpenSelectedItem();

        void lstEvents_ItemMouseHover(object sender, ListViewItemMouseHoverEventArgs e)
        {
            Event ev = (Event)e.Item.Tag;
            toolTip.SetToolTip(lstEvents, GetTooltip(ev));
            toolTip.Hide(lstEvents); // if we don't hide it, it won't work reliably
        }

        void lstEvents_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(!e.Handled && (e.KeyChar == '\r' || e.KeyChar == '\n') && IsEventSelected) // pressing Enter opens the selected event
            {
                OpenSelectedItem();
                e.Handled = true;
            }
        }

        void lstEvents_SelectedIndexChanged(object sender, EventArgs e)
        {
            miOpen.Enabled = cmbTimes.Enabled = btnDismiss.Enabled = btnSnooze.Enabled = IsEventSelected;
            if(!reloadingList && IsEventSelected)
            {
                SuggestSnoozeTime();
                cmbTimes.Focus();
            }
        }

        void miOpen_Click(object sender, EventArgs e) => OpenSelectedItem();
        void miSettings_Click(object sender, EventArgs e) => ShowSettingsForm();

        void miQuit_Click(object sender, EventArgs e)
        {
            quitting = true;
            Close();
        }

        void notifyIcon_BalloonTipClicked(object sender, EventArgs e) => ShowSettingsForm();

        void notifyIcon_DoubleClick(object sender, EventArgs e) => UserShowForm(true);

        void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                if(notifyIcon.Icon == errorIcon) ShowSettingsForm();
                else UserShowForm(false);
            }
        }

        readonly System.Threading.Timer snoozeTimer;
        readonly Dictionary<string, TrackedEvent> trackedEvents = new Dictionary<string, TrackedEvent>();
        readonly Dictionary<string, DateTime> dismissed = new Dictionary<string, DateTime>();
        // Resources.* reloads the image on every property access, but we only want to load them once
        readonly Icon appIcon = Resources.AppIcon, disconnectedIcon = Resources.DisconnectedIcon, errorIcon = Resources.ErrorIcon;
        readonly Bitmap conferenceImg = Resources.Conference, locationImg = Resources.Location, phoneImg = Resources.Phone;
        readonly Bitmap sipImg = Resources.SIP, videoImg = Resources.Video;
        System.Media.SoundPlayer soundPlayer;
        CancellationTokenSource cts = new CancellationTokenSource();
        string lastEventHash;
        EventTracker tracker;
        Task trackerTask;
        DateTime snoozeUntil;
        uint lastPlay = ~(uint)Environment.TickCount; // ensure the last play is very different from the current tick count
        bool reloadingList, startup = true, quitting;

        static int GetReminderMins(Event e)
        {
            DateTime startTime = e.Start.DateTime.Value.ToUniversalTime();
            int minsToStart = (int)(startTime - DateTime.UtcNow).TotalMinutes;
            return minsToStart <= 0 ? 5 : minsToStart < 10 ? 0 : minsToStart < 20 ? -5 : minsToStart < 30 ? -10 :
                minsToStart <= 45 ? -15 : minsToStart < 60 ? 30 : minsToStart < 48*60 ? minsToStart/60*30 :
                minsToStart/(48*60)*(24*60);
        }

        static string GetSnoozeDescription(int minutes)
        {
            bool beforeStart = minutes <= 0;
            if(beforeStart) minutes = -minutes;
            long ticks = minutes * TimeSpan.TicksPerMinute;
            string desc =
                ticks >= TimeSpan.TicksPerDay * 7 ? GetTimeOffset(ticks, TimeSpan.TicksPerDay * 7, "week", false) :
                ticks >= TimeSpan.TicksPerDay ? GetTimeOffset(ticks, TimeSpan.TicksPerDay, "day", false) :
                ticks >= TimeSpan.TicksPerHour ? GetTimeOffset(ticks, TimeSpan.TicksPerHour, "hour", false) :
                GetTimeOffset(ticks, TimeSpan.TicksPerMinute, "minute", false);
            if(beforeStart) desc += " before start";
            return desc;
        }

        static TimeSpan GetTimeOffset(string text)
        {
            Match m = snoozeRe.Match(text);
            if(!m.Success) throw new FormatException();
            double value = double.Parse(m.Groups[1].Value);
            if(m.Groups[2].Success)
            {
                switch(char.ToUpperInvariant(text[m.Groups[2].Index]))
                {
                    case 'H': value *= 60; break;
                    case 'D': value *= 60*24; break;
                    case 'W': value *= 60*24*7; break;
                }
            }
            if(m.Groups[3].Success) value = -value;
            else if(value == 0) throw new FormatException(); // don't accept a zero-minute snooze
            return TimeSpan.FromMinutes(value);
        }

        static string GetTimeOffset(TimeSpan span)
        {
            bool negative = span.Ticks < 0;
            if(negative) span = -span;
            return
                span.Ticks >= TimeSpan.TicksPerDay * 7 ?
                    GetTimeOffset(span.Ticks, TimeSpan.TicksPerDay * 7, "week", TimeSpan.TicksPerDay, "day", 7, negative) :
                span.Ticks >= TimeSpan.TicksPerDay ?
                    GetTimeOffset(span.Ticks, TimeSpan.TicksPerDay, "day", TimeSpan.TicksPerHour, "hour", 24, negative) :
                span.Ticks >= TimeSpan.TicksPerHour ?
                    GetTimeOffset(span.Ticks, TimeSpan.TicksPerHour, "hour", TimeSpan.TicksPerMinute, "minute", 60, negative) :
                span.Ticks >= TimeSpan.TicksPerMinute ? GetTimeOffset(span.Ticks, TimeSpan.TicksPerMinute, "minute", negative, true) :
                "Now";
        }

        static string GetTimeOffset(long ticks, long major, string majorUnit, bool negative, bool round = false)
        {
            double value = (double)ticks / major;
            if(round) value = Math.Round(value);
            var sb = new StringBuilder();
            sb.Append(value.ToString("0.##")).Append(' ').Append(majorUnit);
            if(value != 1) sb.Append('s');
            if(negative) sb.Append(" ago");
            return sb.ToString();
        }

        static string GetTimeOffset(long ticks, long major, string majorUnit, long minor, string minorUnit, uint maxMinor, bool negative)
        {
            uint majorValue = (uint)(ticks / major), minorValue = (uint)Math.Round((double)(ticks % major) / minor);
            if(minorValue == maxMinor)
            {
                majorValue++;
                minorValue = 0;
            }
            var sb = new StringBuilder();
            sb.Append(majorValue).Append(' ').Append(majorUnit);
            if(majorValue > 1) sb.Append('s');
            if(minorValue != 0)
            {
                sb.Append(", ").Append(minorValue).Append(' ').Append(minorUnit);
                if(minorValue > 1) sb.Append('s');
            }
            if(negative) sb.Append(" ago");
            return sb.ToString();
        }

        static string GetTooltip(Event e)
        {
            var sb = new StringBuilder();
            sb.Append("Title: ").Append(e.Summary);
            sb.Append("\nStarts: ").Append(e.Start.DateTime.Value.ToLocalTime().ToString("f"));
            if((e.End?.DateTime).HasValue) sb.Append("\nEnds: ").Append(e.End.DateTime.Value.ToLocalTime().ToString("f"));
            sb.Append("\nCreator: ").Append(e.Creator.DisplayName ?? e.Creator.Email);
            if(!string.IsNullOrEmpty(e.Location) && !IsUrl(e.Location)) sb.Append("\nLocation: ").Append(e.Location);
            return sb.ToString();
        }

        static bool IsUrl(string str) => !string.IsNullOrEmpty(str) && (str.StartsWith("https://") || str.StartsWith("http://"));

        static readonly int[] reminderTimes = new[] {
            -15, -10, -5, 0,
            5, 10, 15, 30, 1*60, 2*60, 3*60, 4*60, 6*60, 8*60, 12*60, 24*60, 2*24*60, 3*24*60, 4*24*60, 5*24*60, 6*24*60, 7*24*60
        };

        static readonly Regex snoozeRe = new Regex(
            @"^\s*([0-9]+(?:\.[0-9]*)?|\.[0-9]+)\s*(m(?:in(?:ute)?s?)?|h(?:ours?)?|d(?:ays?)?|w(?:eeks?)?)\s*(before\s+start)?\s*$",
            RegexOptions.IgnoreCase);
    }
}