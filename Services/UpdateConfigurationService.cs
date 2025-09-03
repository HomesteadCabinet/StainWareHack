using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using StainSelector.Models;

namespace StainSelector.Services
{
    /// <summary>
    /// Service for managing update configuration settings
    /// </summary>
    public class UpdateConfigurationService
    {
        private readonly string _configPath;
        private UpdateConfiguration _configuration;

        public UpdateConfigurationService()
        {
            _configPath = Path.Combine(Application.StartupPath, "update-config.xml");
            _configuration = LoadConfiguration();
        }

        public UpdateConfiguration Configuration => _configuration;

        /// <summary>
        /// Loads configuration from file or creates default configuration
        /// </summary>
        private UpdateConfiguration LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    using var reader = new StreamReader(_configPath);
                    var serializer = new XmlSerializer(typeof(UpdateConfiguration));
                    var config = (UpdateConfiguration?)serializer.Deserialize(reader);
                    return config ?? CreateDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                // Log error and return default configuration
                System.Diagnostics.Debug.WriteLine($"Failed to load update configuration: {ex.Message}");
            }

            return CreateDefaultConfiguration();
        }

        /// <summary>
        /// Creates default configuration
        /// </summary>
        private UpdateConfiguration CreateDefaultConfiguration()
        {
            return new UpdateConfiguration
            {
                GitHubOwner = "JuicyJerry", // Update with your GitHub username
                GitHubRepository = "StainWareHack", // Update with your repository name
                UpdateChannel = "stable",
                CheckForUpdatesOnStartup = true,
                CheckForPrereleases = false,
                CheckFrequencyHours = 24,
                LastUpdateCheck = DateTime.MinValue,
                SkippedVersions = Array.Empty<string>(),
                AutoDownloadUpdates = false,
                AutoInstallUpdates = false,
                DeployKeyPath = Path.Combine(Application.StartupPath, "deploy-key"),
                BackupPath = Path.Combine(Application.StartupPath, "backup")
            };
        }

        /// <summary>
        /// Saves configuration to file
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                using var writer = new StreamWriter(_configPath);
                var serializer = new XmlSerializer(typeof(UpdateConfiguration));
                serializer.Serialize(writer, _configuration);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save update configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates the last update check timestamp
        /// </summary>
        public void UpdateLastCheckTime()
        {
            _configuration.LastUpdateCheck = DateTime.UtcNow;
            SaveConfiguration();
        }

        /// <summary>
        /// Checks if enough time has passed since last update check
        /// </summary>
        public bool ShouldCheckForUpdates()
        {
            if (!_configuration.CheckForUpdatesOnStartup)
                return false;

            var timeSinceLastCheck = DateTime.UtcNow - _configuration.LastUpdateCheck;
            return timeSinceLastCheck.TotalHours >= _configuration.CheckFrequencyHours;
        }

        /// <summary>
        /// Adds a version to the skipped versions list
        /// </summary>
        public void SkipVersion(string version)
        {
            var skippedList = _configuration.SkippedVersions.ToList();
            if (!skippedList.Contains(version))
            {
                skippedList.Add(version);
                _configuration.SkippedVersions = skippedList.ToArray();
                SaveConfiguration();
            }
        }

        /// <summary>
        /// Removes a version from the skipped versions list
        /// </summary>
        public void UnskipVersion(string version)
        {
            var skippedList = _configuration.SkippedVersions.ToList();
            if (skippedList.Remove(version))
            {
                _configuration.SkippedVersions = skippedList.ToArray();
                SaveConfiguration();
            }
        }

        /// <summary>
        /// Checks if a version is in the skipped list
        /// </summary>
        public bool IsVersionSkipped(string version)
        {
            return _configuration.SkippedVersions.Contains(version);
        }

        /// <summary>
        /// Updates GitHub repository information
        /// </summary>
        public void UpdateRepositoryInfo(string owner, string repository)
        {
            _configuration.GitHubOwner = owner;
            _configuration.GitHubRepository = repository;
            SaveConfiguration();
        }

        /// <summary>
        /// Updates update channel preference
        /// </summary>
        public void UpdateChannel(string channel)
        {
            _configuration.UpdateChannel = channel;
            SaveConfiguration();
        }

        /// <summary>
        /// Updates auto-update preferences
        /// </summary>
        public void UpdateAutoUpdateSettings(bool autoDownload, bool autoInstall)
        {
            _configuration.AutoDownloadUpdates = autoDownload;
            _configuration.AutoInstallUpdates = autoInstall;
            SaveConfiguration();
        }
    }
}
