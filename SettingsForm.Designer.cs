namespace CalendarReminder
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.btnConnect = new System.Windows.Forms.Button();
            this.lstCalendars = new System.Windows.Forms.CheckedListBox();
            this.lblCalendars = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkSound = new System.Windows.Forms.CheckBox();
            this.chkStartup = new System.Windows.Forms.CheckBox();
            this.cmbSound = new System.Windows.Forms.ComboBox();
            this.btnPlay = new System.Windows.Forms.Button();
            this.lblConnected = new System.Windows.Forms.Label();
            this.chkDefaultTime = new System.Windows.Forms.CheckBox();
            this.txtDefaultTime = new System.Windows.Forms.TextBox();
            this.lblMinutes = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.cmbOnClose = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(12, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(124, 23);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "&Connect to Google";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // lstCalendars
            // 
            this.lstCalendars.CheckOnClick = true;
            this.lstCalendars.Location = new System.Drawing.Point(12, 61);
            this.lstCalendars.Name = "lstCalendars";
            this.lstCalendars.Size = new System.Drawing.Size(368, 94);
            this.lstCalendars.Sorted = true;
            this.lstCalendars.TabIndex = 3;
            this.toolTip.SetToolTip(this.lstCalendars, "Select the calendars you want to receive notifications for. You must connect to G" +
        "oogle first.");
            this.lstCalendars.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lstCalendars_ItemCheck);
            // 
            // lblCalendars
            // 
            this.lblCalendars.AutoSize = true;
            this.lblCalendars.Location = new System.Drawing.Point(9, 45);
            this.lblCalendars.Name = "lblCalendars";
            this.lblCalendars.Size = new System.Drawing.Size(101, 13);
            this.lblCalendars.TabIndex = 2;
            this.lblCalendars.Text = "Calendars to &Watch";
            this.lblCalendars.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.toolTip.SetToolTip(this.lblCalendars, "Select the calendars you want to receive notifications for. You must connect to G" +
        "oogle first.");
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOK.Location = new System.Drawing.Point(117, 272);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 13;
            this.btnOK.Text = "&OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(198, 272);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 14;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // chkSound
            // 
            this.chkSound.AutoSize = true;
            this.chkSound.Location = new System.Drawing.Point(12, 189);
            this.chkSound.Name = "chkSound";
            this.chkSound.Size = new System.Drawing.Size(121, 17);
            this.chkSound.TabIndex = 7;
            this.chkSound.Text = "Play &sound on alarm";
            this.toolTip.SetToolTip(this.chkSound, "If checked, a sound effect will be played when the program gives you a reminder.");
            this.chkSound.UseVisualStyleBackColor = true;
            this.chkSound.CheckedChanged += new System.EventHandler(this.chkSound_CheckedChanged);
            // 
            // chkStartup
            // 
            this.chkStartup.AutoSize = true;
            this.chkStartup.Location = new System.Drawing.Point(12, 216);
            this.chkStartup.Name = "chkStartup";
            this.chkStartup.Size = new System.Drawing.Size(143, 17);
            this.chkStartup.TabIndex = 10;
            this.chkStartup.Text = "&Run on Windows startup";
            this.toolTip.SetToolTip(this.chkStartup, "If checked, the program will run automatically when you log into Windows.");
            this.chkStartup.UseVisualStyleBackColor = true;
            // 
            // cmbSound
            // 
            this.cmbSound.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSound.Enabled = false;
            this.cmbSound.FormattingEnabled = true;
            this.cmbSound.Items.AddRange(new object[] {
            "(Default)"});
            this.cmbSound.Location = new System.Drawing.Point(139, 187);
            this.cmbSound.Name = "cmbSound";
            this.cmbSound.Size = new System.Drawing.Size(212, 21);
            this.cmbSound.TabIndex = 8;
            // 
            // btnPlay
            // 
            this.btnPlay.Image = global::CalendarReminder.Resources.Play;
            this.btnPlay.Location = new System.Drawing.Point(357, 186);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(23, 23);
            this.btnPlay.TabIndex = 9;
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // lblConnected
            // 
            this.lblConnected.AutoSize = true;
            this.lblConnected.Location = new System.Drawing.Point(143, 17);
            this.lblConnected.Name = "lblConnected";
            this.lblConnected.Size = new System.Drawing.Size(82, 13);
            this.lblConnected.TabIndex = 1;
            this.lblConnected.Text = "(not connected)";
            // 
            // chkDefaultTime
            // 
            this.chkDefaultTime.AutoSize = true;
            this.chkDefaultTime.Checked = true;
            this.chkDefaultTime.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDefaultTime.Location = new System.Drawing.Point(12, 163);
            this.chkDefaultTime.Name = "chkDefaultTime";
            this.chkDefaultTime.Size = new System.Drawing.Size(125, 17);
            this.chkDefaultTime.TabIndex = 4;
            this.chkDefaultTime.Text = "&Default reminder time";
            this.toolTip.SetToolTip(this.chkDefaultTime, resources.GetString("chkDefaultTime.ToolTip"));
            this.chkDefaultTime.UseVisualStyleBackColor = true;
            this.chkDefaultTime.CheckedChanged += new System.EventHandler(this.chkOverrideTime_CheckedChanged);
            // 
            // txtDefaultTime
            // 
            this.txtDefaultTime.Location = new System.Drawing.Point(139, 161);
            this.txtDefaultTime.Name = "txtDefaultTime";
            this.txtDefaultTime.Size = new System.Drawing.Size(42, 20);
            this.txtDefaultTime.TabIndex = 5;
            this.toolTip.SetToolTip(this.txtDefaultTime, resources.GetString("txtDefaultTime.ToolTip"));
            // 
            // lblMinutes
            // 
            this.lblMinutes.AutoSize = true;
            this.lblMinutes.Location = new System.Drawing.Point(183, 165);
            this.lblMinutes.Name = "lblMinutes";
            this.lblMinutes.Size = new System.Drawing.Size(43, 13);
            this.lblMinutes.TabIndex = 6;
            this.lblMinutes.Text = "minutes";
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 10000;
            this.toolTip.InitialDelay = 500;
            this.toolTip.ReshowDelay = 100;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 244);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "&Upon closing window";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbOnClose
            // 
            this.cmbOnClose.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOnClose.FormattingEnabled = true;
            this.cmbOnClose.Items.AddRange(new object[] {
            "Ask",
            "Snooze all",
            "Dismiss all"});
            this.cmbOnClose.Location = new System.Drawing.Point(139, 240);
            this.cmbOnClose.Name = "cmbOnClose";
            this.cmbOnClose.Size = new System.Drawing.Size(87, 21);
            this.cmbOnClose.TabIndex = 12;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(392, 307);
            this.Controls.Add(this.cmbOnClose);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblMinutes);
            this.Controls.Add(this.txtDefaultTime);
            this.Controls.Add(this.chkDefaultTime);
            this.Controls.Add(this.lblConnected);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.cmbSound);
            this.Controls.Add(this.chkStartup);
            this.Controls.Add(this.chkSound);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblCalendars);
            this.Controls.Add(this.lstCalendars);
            this.Controls.Add(this.btnConnect);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "SettingsForm";
            this.Text = "Google Calendar Reminder - Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.CheckedListBox lstCalendars;
        private System.Windows.Forms.Label lblCalendars;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkSound;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.ComboBox cmbSound;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Label lblConnected;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox chkDefaultTime;
        private System.Windows.Forms.TextBox txtDefaultTime;
        private System.Windows.Forms.Label lblMinutes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbOnClose;
    }
}