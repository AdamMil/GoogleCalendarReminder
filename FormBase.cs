using System;
using System.Threading;
using System.Windows.Forms;

namespace CalendarReminder
{
    class FormBase : Form
    {
        protected FormBase() => syncContext = SynchronizationContext.Current;

        protected void QueueInvoke(Action action) => syncContext.Post(_ => action(), null);

        protected void ShowError(string text, string caption = null) =>
            MessageBox.Show(this, text, caption ?? "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);

        protected void ShowError(Exception ex, string text) => ShowError($"{text}. {ex.GetType().Name} - {ex.Message}");

        readonly SynchronizationContext syncContext;
    }
}
