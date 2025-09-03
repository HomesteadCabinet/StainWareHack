using System;
using System.Drawing;
using System.Windows.Forms;
using StainSelector.Models;
using StainSelector.Services;

namespace StainSelector
{
    /// <summary>
    /// Dialog for configuring update settings
    /// </summary>
    public partial class UpdateSettingsDialog : Form
    {
        private readonly UpdateConfigurationService _configService;
        private UpdateConfiguration _configuration;

        // UI Controls
        private CheckBox _checkOnStartupCheckBox = null!;
        private CheckBox _autoDownloadCheckBox = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;

        public UpdateSettingsDialog(UpdateConfigurationService configService)
        {
            _configService = configService;
            _configuration = new UpdateConfiguration
            {
                CheckForUpdatesOnStartup = _configService.Configuration.CheckForUpdatesOnStartup,
                AutoDownloadUpdates = _configService.Configuration.AutoDownloadUpdates
            };

            InitializeComponent();
            PopulateSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Update Settings";
            this.Size = new Size(350, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);

            // Main container
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "Update Settings",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 48),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Update Settings
            var updateGroupBox = new GroupBox
            {
                Text = "Update Preferences",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 60),
                Size = new Size(290, 80)
            };

            _checkOnStartupCheckBox = new CheckBox
            {
                Text = "Check for updates on startup",
                Location = new Point(15, 25),
                Size = new Size(250, 20),
                Font = new Font("Segoe UI", 9)
            };

            _autoDownloadCheckBox = new CheckBox
            {
                Text = "Automatically download updates",
                Location = new Point(15, 50),
                Size = new Size(250, 20),
                Font = new Font("Segoe UI", 9)
            };

            updateGroupBox.Controls.Add(_checkOnStartupCheckBox);
            updateGroupBox.Controls.Add(_autoDownloadCheckBox);

            // Buttons
            var buttonsPanel = new Panel
            {
                Location = new Point(20, 180),
                Size = new Size(290, 40)
            };

            _okButton = new Button
            {
                Text = "OK",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(100, 5)
            };
            _okButton.FlatAppearance.BorderSize = 0;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(45, 45, 48),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Location = new Point(190, 5)
            };
            _cancelButton.FlatAppearance.BorderSize = 0;

            buttonsPanel.Controls.Add(_okButton);
            buttonsPanel.Controls.Add(_cancelButton);

            // Add all controls to main panel
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(updateGroupBox);
            mainPanel.Controls.Add(buttonsPanel);

            this.Controls.Add(mainPanel);

            // Event handlers
            _okButton.Click += OkButton_Click;
            _cancelButton.Click += CancelButton_Click;
        }

        private void PopulateSettings()
        {
            _checkOnStartupCheckBox.Checked = _configuration.CheckForUpdatesOnStartup;
            _autoDownloadCheckBox.Checked = _configuration.AutoDownloadUpdates;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            // Update configuration
            _configService.UpdateAutoUpdateSettings(_autoDownloadCheckBox.Checked, _autoDownloadCheckBox.Checked);
            _configService.Configuration.CheckForUpdatesOnStartup = _checkOnStartupCheckBox.Checked;
            _configService.SaveConfiguration();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
