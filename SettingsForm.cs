using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Win32;

namespace CalendarReminder
{
    partial class SettingsForm : FormBase
    {
        public SettingsForm()
        {
            InitializeComponent();
            lstCalendars.DisplayMember = "Summary";
        }

        public SettingsForm(CalendarService service) : this() => Service = service;

        public string[] CalendarIds { get; private set; }
        public CalendarService Service { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Icon = Resources.AppIcon;

            int defaultReminder = Program.DataStore.Get<int>(Settings.DefaultReminder);
            if(defaultReminder < 0) chkDefaultTime.Checked = false;
            else txtDefaultTime.Text = defaultReminder > 0 ? defaultReminder.ToString() : string.Empty;

            string soundFile = Program.DataStore.Get<string>(Settings.PlaySound);
            chkSound.Checked = soundFile != null;
            int soundIndex = 0;
            try
            {
                string mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
                if(Directory.Exists(mediaDir))
                {
                    soundPaths = Directory.GetFiles(mediaDir, "*.wav");
                    string[] names = new string[soundPaths.Length];
                    for(int i = 0; i < names.Length; i++) names[i] = Path.GetFileNameWithoutExtension(soundPaths[i]);
                    Array.Sort(names, soundPaths, StringComparer.CurrentCultureIgnoreCase);
                    soundIndex = Array.IndexOf(soundPaths, soundFile) + 1;
                    cmbSound.Items.AddRange(names);
                }
            }
            catch // ignore errors loading the windows sound list
            {
            }
            cmbSound.SelectedIndex = soundIndex;

            using(RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                string registeredPath = key?.GetValue("Google Calendar Reminder") as string;
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                chkStartup.Checked = registeredPath == exePath;
            }

            Enum.TryParse(Program.DataStore.Get<string>(Settings.OnClose), out OnCloseBehavior onClose);
            cmbOnClose.SelectedIndex = (int)onClose;

            EnableButtons();

            if(Service != null) // if we're already connected...
            {
                lblConnected.Text = "(connected)";
                cts = new CancellationTokenSource();
                Task.Run(() => LoadCalendars(cts.Token));
            }
        }

        async Task Connect(CancellationToken cancelToken)
        {
            tempStore = new JsonDataStore(null);
            Service = null;
            try
            {
                Service = await Program.ConnectAsync(tempStore, cancelToken).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                if(!(ex is OperationCanceledException)) Invoke((Action)(() => ShowError(ex, "Unable to connect account")));
                goto done;
            }

            try
            {
                await LoadCalendars(cancelToken).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                if(!(ex is OperationCanceledException)) Invoke((Action)(() => ShowError(ex, "Unable to load calendars")));
                goto done;
            }

            done:
            Invoke((Action)(() =>
            {
                lblConnected.Text = Service != null ? "(connected)" : "(not connected)";
                btnConnect.Text = "&Connect to Google";
                EnableButtons();
            }));
        }

        void EnableButtons(bool? checkedItems = null) =>
            btnOK.Enabled = Service != null && (checkedItems ?? lstCalendars.CheckedItems.Count != 0);

        async Task LoadCalendars(CancellationToken cancelToken)
        {
            var calendars = await Service.CalendarList.List().ExecuteAsync(cancelToken).ConfigureAwait(false);
            var selectedIds = Program.DataStore.Get<string[]>(Settings.CalendarIds);
            Invoke((Action)(() =>
            {
                lstCalendars.SuspendLayout(); // suspend sorting until we're done
                lstCalendars.Items.Clear();
                foreach(CalendarListEntry calendar in calendars.Items)
                {
                    bool selected = selectedIds == null ? calendar.Primary == true : Array.IndexOf(selectedIds, calendar.Id) >= 0;
                    lstCalendars.Items.Add(calendar, selected);
                }
                lstCalendars.ResumeLayout();
            }));
        }

        void btnConnect_Click(object sender, EventArgs e)
        {
            if(btnConnect.Text == "Cancel")
            {
                cts.Cancel();
            }
            else
            {
                btnConnect.Text = "Cancel";
                cts = new CancellationTokenSource();
                connectTask = Connect(cts.Token);
            }
        }

        void btnOK_Click(object sender, EventArgs e)
        {
            int defaultReminder = chkDefaultTime.Checked ? 0 : -1;
            if(chkDefaultTime.Checked && !string.IsNullOrWhiteSpace(txtDefaultTime.Text) &&
               (!int.TryParse(txtDefaultTime.Text, out defaultReminder) || defaultReminder <= 0))
            {
                ShowError($"\"{txtDefaultTime.Text}\" is not a valid reminder time. It must be a positive integer (e.g. 30).");
                txtDefaultTime.Focus();
                return;
            }
            if(defaultReminder == 0) Program.DataStore.Delete<int>(Settings.DefaultReminder);
            else Program.DataStore.Set(Settings.DefaultReminder, defaultReminder);

            tempStore?.CopyTo(Program.DataStore); // copy authentication settings, if any
            CalendarIds = lstCalendars.CheckedItems.Cast<CalendarListEntry>().Select(c => c.Id).ToArray();
            Program.DataStore.Set(Settings.CalendarIds, CalendarIds);

            if(chkSound.Checked)
            {
                Program.DataStore.Set(Settings.PlaySound,
                    cmbSound.SelectedIndex == 0 ? string.Empty : soundPaths[cmbSound.SelectedIndex-1]);
            }
            else
            {
                Program.DataStore.Delete<string>(Settings.PlaySound);
            }

            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                const string ValueName = "Google Calendar Reminder";
                if(chkStartup.Checked) key.SetValue(ValueName, System.Reflection.Assembly.GetExecutingAssembly().Location);
                else key.DeleteValue(ValueName);
            }

            Program.DataStore.Set(Settings.OnClose, ((OnCloseBehavior)cmbOnClose.SelectedIndex).ToString());

            DialogResult = DialogResult.OK;
        }

        void btnPlay_Click(object sender, EventArgs e)
        {
            try // TODO: stop the previous sound when playing a new one? (it seems to happen automatically, but maybe we should be explicit about it...)
            {
                if(cmbSound.SelectedIndex == 0)
                {
                    using(var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CalendarReminder.Resources.alarm.wav"))
                    {
                        new System.Media.SoundPlayer(stream).Play();
                    }
                }
                else
                {
                    using(var stream = File.OpenRead(soundPaths[cmbSound.SelectedIndex-1]))
                    {
                        new System.Media.SoundPlayer(stream).Play();
                    }
                }
            }
            catch(Exception ex)
            {
                ShowError(ex, "Error playing sound");
            }
        }

        void chkOverrideTime_CheckedChanged(object sender, EventArgs e) => txtDefaultTime.Enabled = chkDefaultTime.Checked;
        void chkSound_CheckedChanged(object sender, EventArgs e) => btnPlay.Enabled = cmbSound.Enabled = chkSound.Checked;

        void lstCalendars_ItemCheck(object sender, ItemCheckEventArgs e) =>
            EnableButtons(e.NewValue == CheckState.Checked || lstCalendars.CheckedItems.Count > 1);

        JsonDataStore tempStore;
        string[] soundPaths;
        CancellationTokenSource cts;
        Task connectTask;
    }
}