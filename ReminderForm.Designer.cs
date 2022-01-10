namespace CalendarReminder
{
    partial class ReminderForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lstEvents = new System.Windows.Forms.ListView();
            this.colEvent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.itemContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.cmbTimes = new System.Windows.Forms.ComboBox();
            this.btnSnooze = new System.Windows.Forms.Button();
            this.btnDismiss = new System.Windows.Forms.Button();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.appContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.miShow = new System.Windows.Forms.ToolStripMenuItem();
            this.miSep = new System.Windows.Forms.ToolStripSeparator();
            this.miQuit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.eventTimer = new System.Windows.Forms.Timer(this.components);
            this.btnDismissAll = new System.Windows.Forms.Button();
            this.enableTimer = new System.Windows.Forms.Timer(this.components);
            this.itemContextMenu.SuspendLayout();
            this.appContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstEvents
            // 
            this.lstEvents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstEvents.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colEvent,
            this.colTime});
            this.lstEvents.ContextMenuStrip = this.itemContextMenu;
            this.lstEvents.FullRowSelect = true;
            this.lstEvents.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstEvents.HideSelection = false;
            this.lstEvents.LabelWrap = false;
            this.lstEvents.Location = new System.Drawing.Point(13, 13);
            this.lstEvents.MultiSelect = false;
            this.lstEvents.Name = "lstEvents";
            this.lstEvents.ShowGroups = false;
            this.lstEvents.Size = new System.Drawing.Size(459, 226);
            this.lstEvents.TabIndex = 0;
            this.lstEvents.UseCompatibleStateImageBehavior = false;
            this.lstEvents.View = System.Windows.Forms.View.Details;
            this.lstEvents.ItemMouseHover += new System.Windows.Forms.ListViewItemMouseHoverEventHandler(this.lstEvents_ItemMouseHover);
            this.lstEvents.SelectedIndexChanged += new System.EventHandler(this.lstEvents_SelectedIndexChanged);
            this.lstEvents.DoubleClick += new System.EventHandler(this.lstEvents_DoubleClick);
            this.lstEvents.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.lstEvents_KeyPress);
            // 
            // colEvent
            // 
            this.colEvent.Text = "Summary";
            // 
            // colTime
            // 
            this.colTime.Text = "Start Time";
            this.colTime.Width = 150;
            // 
            // itemContextMenu
            // 
            this.itemContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miOpen});
            this.itemContextMenu.Name = "itemContextMenu";
            this.itemContextMenu.Size = new System.Drawing.Size(136, 26);
            this.itemContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.itemContextMenu_Opening);
            // 
            // miOpen
            // 
            this.miOpen.Enabled = false;
            this.miOpen.Image = global::CalendarReminder.Resources.Event;
            this.miOpen.Name = "miOpen";
            this.miOpen.Size = new System.Drawing.Size(135, 22);
            this.miOpen.Text = "&Open Event";
            this.miOpen.Click += new System.EventHandler(this.miOpen_Click);
            // 
            // cmbTimes
            // 
            this.cmbTimes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTimes.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append;
            this.cmbTimes.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbTimes.FormattingEnabled = true;
            this.cmbTimes.Location = new System.Drawing.Point(13, 246);
            this.cmbTimes.Name = "cmbTimes";
            this.cmbTimes.Size = new System.Drawing.Size(216, 21);
            this.cmbTimes.TabIndex = 1;
            // 
            // btnSnooze
            // 
            this.btnSnooze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSnooze.Location = new System.Drawing.Point(235, 245);
            this.btnSnooze.Name = "btnSnooze";
            this.btnSnooze.Size = new System.Drawing.Size(75, 23);
            this.btnSnooze.TabIndex = 2;
            this.btnSnooze.Text = "&Snooze";
            this.btnSnooze.UseVisualStyleBackColor = true;
            this.btnSnooze.Click += new System.EventHandler(this.btnSnooze_Click);
            // 
            // btnDismiss
            // 
            this.btnDismiss.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDismiss.Location = new System.Drawing.Point(316, 245);
            this.btnDismiss.Name = "btnDismiss";
            this.btnDismiss.Size = new System.Drawing.Size(75, 23);
            this.btnDismiss.TabIndex = 3;
            this.btnDismiss.Text = "&Dismiss";
            this.btnDismiss.UseVisualStyleBackColor = true;
            this.btnDismiss.Click += new System.EventHandler(this.btnDismiss_Click);
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.appContextMenu;
            this.notifyIcon.Visible = true;
            this.notifyIcon.BalloonTipClicked += new System.EventHandler(this.notifyIcon_BalloonTipClicked);
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            // 
            // appContextMenu
            // 
            this.appContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miSettings,
            this.miShow,
            this.miSep,
            this.miQuit});
            this.appContextMenu.Name = "contextMenu";
            this.appContextMenu.Size = new System.Drawing.Size(117, 76);
            // 
            // miSettings
            // 
            this.miSettings.Name = "miSettings";
            this.miSettings.Size = new System.Drawing.Size(116, 22);
            this.miSettings.Text = "&Settings";
            this.miSettings.Click += new System.EventHandler(this.miSettings_Click);
            // 
            // miShow
            // 
            this.miShow.Enabled = false;
            this.miShow.Name = "miShow";
            this.miShow.Size = new System.Drawing.Size(116, 22);
            this.miShow.Text = "Show";
            // 
            // miSep
            // 
            this.miSep.Name = "miSep";
            this.miSep.Size = new System.Drawing.Size(113, 6);
            // 
            // miQuit
            // 
            this.miQuit.Name = "miQuit";
            this.miQuit.Size = new System.Drawing.Size(116, 22);
            this.miQuit.Text = "&Quit";
            this.miQuit.Click += new System.EventHandler(this.miQuit_Click);
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 0;
            this.toolTip.ReshowDelay = 0;
            // 
            // eventTimer
            // 
            this.eventTimer.Enabled = true;
            this.eventTimer.Interval = 60000;
            this.eventTimer.Tick += new System.EventHandler(this.eventTimer_Tick);
            // 
            // btnDismissAll
            // 
            this.btnDismissAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDismissAll.Location = new System.Drawing.Point(397, 245);
            this.btnDismissAll.Name = "btnDismissAll";
            this.btnDismissAll.Size = new System.Drawing.Size(75, 23);
            this.btnDismissAll.TabIndex = 4;
            this.btnDismissAll.Text = "Dismiss &All";
            this.btnDismissAll.UseVisualStyleBackColor = true;
            this.btnDismissAll.Click += new System.EventHandler(this.btnDismissAll_Click);
            // 
            // enableTimer
            // 
            this.enableTimer.Interval = 500;
            this.enableTimer.Tick += new System.EventHandler(this.enableTimer_Tick);
            // 
            // ReminderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 280);
            this.Controls.Add(this.btnDismissAll);
            this.Controls.Add(this.btnDismiss);
            this.Controls.Add(this.btnSnooze);
            this.Controls.Add(this.cmbTimes);
            this.Controls.Add(this.lstEvents);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(408, 286);
            this.Name = "ReminderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Google Calendar Reminder";
            this.TopMost = true;
            this.itemContextMenu.ResumeLayout(false);
            this.appContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView lstEvents;
        private System.Windows.Forms.ColumnHeader colEvent;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ComboBox cmbTimes;
        private System.Windows.Forms.Button btnSnooze;
        private System.Windows.Forms.Button btnDismiss;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip appContextMenu;
        private System.Windows.Forms.ToolStripMenuItem miSettings;
        private System.Windows.Forms.ToolStripMenuItem miQuit;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Timer eventTimer;
        private System.Windows.Forms.Button btnDismissAll;
        private System.Windows.Forms.ContextMenuStrip itemContextMenu;
        private System.Windows.Forms.ToolStripMenuItem miOpen;
        private System.Windows.Forms.ToolStripMenuItem miShow;
        private System.Windows.Forms.ToolStripSeparator miSep;
        private System.Windows.Forms.Timer enableTimer;
    }
}