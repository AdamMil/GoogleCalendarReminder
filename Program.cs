using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Win32;

namespace CalendarReminder
{
    static class Program
    {
        public static event Action Resume;
        public static JsonDataStore DataStore { get; private set; }

        public static async Task<CalendarService> ConnectAsync(IDataStore store, CancellationToken cancelToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            cts.CancelAfter(TimeSpan.FromSeconds(60));
            var secrets = new ClientSecrets() {
                ClientId = "265724120992-b9s829kos5cjhsja2kogrv977kcennfs.apps.googleusercontent.com",
                ClientSecret = "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"
            };
            var perms = new[] { CalendarService.Scope.CalendarReadonly, CalendarService.Scope.CalendarEventsReadonly };
            var creds = await GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, perms, "user", cts.Token, store).ConfigureAwait(false);
            return new CalendarService(
                new BaseClientService.Initializer() { HttpClientInitializer = creds, ApplicationName = "Google Calendar Reminder" });
        }

        [STAThread]
        static void Main()
        {
            SystemEvents.PowerModeChanged += (o, e) =>
            {
                if(e.Mode == PowerModes.Resume) Resume?.Invoke();
            };
            SystemEvents.SessionSwitch += (o, e) => // PowerModeChange is not reliable, so use session unlock as a proxy for waking up
            {
                if(e.Reason == SessionSwitchReason.SessionUnlock) Resume?.Invoke();
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                DataStore = new JsonDataStore("GoogleCalendarReminder");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Unable to create or access application data folder. {ex.GetType().Name} - {ex.Message}",
                    "Google Calendar Reminder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            form = new ReminderForm() { WindowState = FormWindowState.Minimized };
            Application.Run(form);
        }

        static ReminderForm form;
    }
}