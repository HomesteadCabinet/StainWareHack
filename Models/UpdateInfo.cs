using System;

namespace StainSelector.Models
{
    /// <summary>
    /// Represents information about an available update
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public bool IsUpdateAvailable { get; set; }
        public bool IsPrerelease { get; set; }
        public string Channel { get; set; } = "stable";
    }

    /// <summary>
    /// Represents the current application version information
    /// </summary>
    public class CurrentVersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Channel { get; set; } = "stable";
        public DateTime InstallDate { get; set; }
        public string InstallPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents update configuration settings
    /// </summary>
    public class UpdateConfiguration
    {
        public string GitHubOwner { get; set; } = string.Empty;
        public string GitHubRepository { get; set; } = string.Empty;
        public string UpdateChannel { get; set; } = "stable";
        public bool CheckForUpdatesOnStartup { get; set; } = true;
        public bool CheckForPrereleases { get; set; } = false;
        public int CheckFrequencyHours { get; set; } = 24;
        public DateTime LastUpdateCheck { get; set; }
        public string[] SkippedVersions { get; set; } = Array.Empty<string>();
        public bool AutoDownloadUpdates { get; set; } = false;
        public bool AutoInstallUpdates { get; set; } = false;
        public string DeployKeyPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = "backup";
    }

    /// <summary>
    /// Represents the update manifest XML structure
    /// </summary>
    public class UpdateManifest
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public string Channel { get; set; } = "stable";
        public bool IsPrerelease { get; set; }
    }

    /// <summary>
    /// Represents the result of an update operation
    /// </summary>
    public class UpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public string? BackupPath { get; set; }
        public bool RequiresRestart { get; set; }
    }

    /// <summary>
    /// Represents download progress information
    /// </summary>
    public class DownloadProgress
    {
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
        public string Status { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public bool IsCancelled { get; set; }
    }
}
