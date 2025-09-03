using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using StainSelector.Models;
using StainSelector.Services;

namespace StainSelector
{
    /// <summary>
    /// Dialog for displaying update information and managing the update process
    /// </summary>
    public partial class PortableUpdateDialog : Form
    {
        private readonly UpdateInfo _updateInfo;
        private readonly PortableUpdateManager _updateManager;
        private readonly UpdateConfigurationService _configService;
        private bool _isDownloading;
        private bool _isInstalling;
        private string? _downloadedFilePath;

        // UI Controls
        private Label _titleLabel = null!;
        private Label _versionLabel = null!;
        private Label _releaseNotesLabel = null!;
        private TextBox _releaseNotesTextBox = null!;
        private Label _fileSizeLabel = null!;
        private Label _releaseDateLabel = null!;
        private ProgressBar _progressBar = null!;
        private Label _statusLabel = null!;
        private Button _downloadButton = null!;
        private Button _installButton = null!;
        private Button _skipButton = null!;
        private Button _remindLaterButton = null!;
        private Button _cancelButton = null!;
        private CheckBox _autoUpdateCheckBox = null!;

        public PortableUpdateDialog(UpdateInfo updateInfo)
        {
            _updateInfo = updateInfo;
            _updateManager = new PortableUpdateManager();
            _configService = new UpdateConfigurationService();

            InitializeComponent();
            SetupEventHandlers();
            PopulateUpdateInfo();
        }

        private void InitializeComponent()
        {
            this.Text = "Update Available";
            this.Size = new Size(500, 600);
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
            _titleLabel = new Label
            {
                Text = "Update Available",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 48),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Version info
            _versionLabel = new Label
            {
                Text = $"Version {_updateInfo.Version} is available",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(45, 45, 48),
                AutoSize = true,
                Location = new Point(20, 60)
            };

            // File size and release date
            var infoPanel = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(440, 40),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _fileSizeLabel = new Label
            {
                Text = $"File Size: {FormatFileSize(_updateInfo.FileSize)}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(10, 10),
                AutoSize = true
            };

            _releaseDateLabel = new Label
            {
                Text = $"Released: {_updateInfo.ReleaseDate:yyyy-MM-dd}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(10, 25),
                AutoSize = true
            };

            infoPanel.Controls.Add(_fileSizeLabel);
            infoPanel.Controls.Add(_releaseDateLabel);

            // Release notes
            _releaseNotesLabel = new Label
            {
                Text = "Release Notes:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 48),
                Location = new Point(20, 150),
                AutoSize = true
            };

            _releaseNotesTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(20, 175),
                Size = new Size(440, 150),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Progress bar
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 340),
                Size = new Size(440, 23),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            // Status label
            _statusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, 370),
                Size = new Size(440, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Auto-update checkbox
            _autoUpdateCheckBox = new CheckBox
            {
                Text = "Automatically download and install updates",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 400),
                Size = new Size(440, 20),
                Checked = _configService.Configuration.AutoDownloadUpdates
            };

            // Buttons panel
            var buttonsPanel = new Panel
            {
                Location = new Point(20, 430),
                Size = new Size(440, 50)
            };

            _downloadButton = new Button
            {
                Text = "Download Update",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 35),
                Location = new Point(0, 8)
            };
            _downloadButton.FlatAppearance.BorderSize = 0;

            _installButton = new Button
            {
                Text = "Install Update",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 35),
                Location = new Point(130, 8),
                Enabled = false
            };
            _installButton.FlatAppearance.BorderSize = 0;

            _skipButton = new Button
            {
                Text = "Skip This Version",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(45, 45, 48),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 35),
                Location = new Point(260, 8)
            };
            _skipButton.FlatAppearance.BorderSize = 0;

            _remindLaterButton = new Button
            {
                Text = "Remind Later",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(45, 45, 48),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 35),
                Location = new Point(390, 8)
            };
            _remindLaterButton.FlatAppearance.BorderSize = 0;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(45, 45, 48),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 35),
                Location = new Point(500, 8),
                Visible = false
            };
            _cancelButton.FlatAppearance.BorderSize = 0;

            buttonsPanel.Controls.Add(_downloadButton);
            buttonsPanel.Controls.Add(_installButton);
            buttonsPanel.Controls.Add(_skipButton);
            buttonsPanel.Controls.Add(_remindLaterButton);
            buttonsPanel.Controls.Add(_cancelButton);

            // Add all controls to main panel
            mainPanel.Controls.Add(_titleLabel);
            mainPanel.Controls.Add(_versionLabel);
            mainPanel.Controls.Add(infoPanel);
            mainPanel.Controls.Add(_releaseNotesLabel);
            mainPanel.Controls.Add(_releaseNotesTextBox);
            mainPanel.Controls.Add(_progressBar);
            mainPanel.Controls.Add(_statusLabel);
            mainPanel.Controls.Add(_autoUpdateCheckBox);
            mainPanel.Controls.Add(buttonsPanel);

            this.Controls.Add(mainPanel);
        }

        private void SetupEventHandlers()
        {
            _downloadButton.Click += DownloadButton_Click;
            _installButton.Click += InstallButton_Click;
            _skipButton.Click += SkipButton_Click;
            _remindLaterButton.Click += RemindLaterButton_Click;
            _cancelButton.Click += CancelButton_Click;
            _autoUpdateCheckBox.CheckedChanged += AutoUpdateCheckBox_CheckedChanged;

            _updateManager.DownloadProgressChanged += UpdateManager_DownloadProgressChanged;
            _updateManager.StatusChanged += UpdateManager_StatusChanged;
        }

        private void PopulateUpdateInfo()
        {
            _releaseNotesTextBox.Text = _updateInfo.ReleaseNotes;
            _fileSizeLabel.Text = $"File Size: {FormatFileSize(_updateInfo.FileSize)}";
            _releaseDateLabel.Text = $"Released: {_updateInfo.ReleaseDate:yyyy-MM-dd}";

            if (_updateInfo.IsPrerelease)
            {
                _versionLabel.Text += " (Pre-release)";
                _versionLabel.ForeColor = Color.FromArgb(255, 140, 0);
            }
        }

        private async void DownloadButton_Click(object? sender, EventArgs e)
        {
            if (_isDownloading) return;

            _isDownloading = true;
            _downloadButton.Enabled = false;
            _progressBar.Visible = true;
            _progressBar.Style = ProgressBarStyle.Marquee;
            _cancelButton.Visible = true;

            try
            {
                var progress = new Progress<DownloadProgress>(UpdateDownloadProgress);
                var result = await _updateManager.DownloadUpdateAsync(_updateInfo, progress);

                if (result.Success)
                {
                    _downloadedFilePath = Path.Combine(Path.GetTempPath(), $"StainSelector-{_updateInfo.Version}.zip");
                    _installButton.Enabled = true;
                    _statusLabel.Text = "Download completed successfully";
                    _progressBar.Style = ProgressBarStyle.Continuous;
                    _progressBar.Value = 100;
                }
                else
                {
                    MessageBox.Show($"Download failed: {result.Message}", "Download Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = $"Download failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}", "Download Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = $"Download failed: {ex.Message}";
            }
            finally
            {
                _isDownloading = false;
                _downloadButton.Enabled = true;
                _cancelButton.Visible = false;
            }
        }

        private async void InstallButton_Click(object? sender, EventArgs e)
        {
            if (_isInstalling || string.IsNullOrEmpty(_downloadedFilePath)) return;

            _isInstalling = true;
            _installButton.Enabled = false;
            _progressBar.Visible = true;
            _progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                var result = await _updateManager.InstallUpdateAsync(_downloadedFilePath, _updateInfo);

                if (result.Success)
                {
                    var restartResult = MessageBox.Show(
                        "Update installed successfully. The application needs to restart to complete the update.\n\n" +
                        "Do you want to restart now?",
                        "Update Complete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (restartResult == DialogResult.Yes)
                    {
                        _updateManager.RestartApplication();
                    }
                    else
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show($"Installation failed: {result.Message}", "Installation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _statusLabel.Text = $"Installation failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Installation failed: {ex.Message}", "Installation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = $"Installation failed: {ex.Message}";
            }
            finally
            {
                _isInstalling = false;
                _installButton.Enabled = true;
            }
        }

        private void SkipButton_Click(object? sender, EventArgs e)
        {
            _configService.SkipVersion(_updateInfo.Version);
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void RemindLaterButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            _updateManager.CancelOperation();
        }

        private void AutoUpdateCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _configService.UpdateAutoUpdateSettings(_autoUpdateCheckBox.Checked, _autoUpdateCheckBox.Checked);
        }

        private void UpdateDownloadProgress(DownloadProgress progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DownloadProgress>(UpdateDownloadProgress), progress);
                return;
            }

            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = (int)progress.Percentage;
            _statusLabel.Text = progress.Status;
        }

        private void UpdateManager_StatusChanged(object? sender, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object?, string>(UpdateManager_StatusChanged), sender, status);
                return;
            }

            _statusLabel.Text = status;
        }

        private void UpdateManager_DownloadProgressChanged(object? sender, DownloadProgress progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object?, DownloadProgress>(UpdateManager_DownloadProgressChanged), sender, progress);
                return;
            }

            UpdateDownloadProgress(progress);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDownloading || _isInstalling)
            {
                var result = MessageBox.Show(
                    "An update operation is in progress. Are you sure you want to cancel?",
                    "Cancel Update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                _updateManager.CancelOperation();
            }

            _updateManager.Dispose();
            base.OnFormClosing(e);
        }
    }
}
