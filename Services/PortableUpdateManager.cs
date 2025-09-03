using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using StainSelector.Models;

namespace StainSelector.Services
{
    /// <summary>
    /// Manages portable application updates with GitHub integration
    /// </summary>
    public class PortableUpdateManager
    {
        private readonly UpdateConfigurationService _configService;
        private readonly DeployKeyManager _deployKeyManager;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cancellationTokenSource;

        public event EventHandler<DownloadProgress>? DownloadProgressChanged;
        public event EventHandler<string>? StatusChanged;

        public PortableUpdateManager()
        {
            _configService = new UpdateConfigurationService();
            _deployKeyManager = new DeployKeyManager(_configService.Configuration.DeployKeyPath);
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"StainSelector/{GetCurrentVersion()}");
        }

        /// <summary>
        /// Gets the current application version
        /// </summary>
        public static string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString(3) ?? "1.0.0";
        }

        /// <summary>
        /// Checks for available updates
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                StatusChanged?.Invoke(this, "Checking for updates...");

                var config = _configService.Configuration;

                // Check if we should skip this check
                if (!_configService.ShouldCheckForUpdates())
                {
                    StatusChanged?.Invoke(this, "Update check skipped (too recent)");
                    return null;
                }

                // Try to get update info from GitHub API
                var updateInfo = await GetUpdateInfoFromGitHubAsync(config.GitHubOwner, config.GitHubRepository, config.UpdateChannel);

                if (updateInfo == null)
                {
                    StatusChanged?.Invoke(this, "No updates available");
                    _configService.UpdateLastCheckTime();
                    return null;
                }

                // Check if this version is skipped
                if (_configService.IsVersionSkipped(updateInfo.Version))
                {
                    StatusChanged?.Invoke(this, $"Version {updateInfo.Version} is skipped");
                    return null;
                }

                // Check if update is available
                var currentVersion = GetCurrentVersion();
                updateInfo.IsUpdateAvailable = IsNewerVersion(updateInfo.Version, currentVersion);

                if (!updateInfo.IsUpdateAvailable)
                {
                    StatusChanged?.Invoke(this, "Application is up to date");
                    _configService.UpdateLastCheckTime();
                    return null;
                }

                StatusChanged?.Invoke(this, $"Update available: {updateInfo.Version}");
                _configService.UpdateLastCheckTime();
                return updateInfo;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Update check failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Gets update information from GitHub API
        /// </summary>
        private async Task<UpdateInfo?> GetUpdateInfoFromGitHubAsync(string owner, string repository, string channel)
        {
            try
            {
                // Try to get update.xml first (for custom update manifests)
                var updateXmlUrl = $"https://github.com/{owner}/{repository}/releases/latest/download/update.xml";
                var updateInfo = await GetUpdateInfoFromXmlAsync(updateXmlUrl);

                if (updateInfo != null)
                {
                    return updateInfo;
                }

                // Fallback to GitHub API
                return await GetUpdateInfoFromGitHubApiAsync(owner, repository, channel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get update info from GitHub: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets update information from update.xml manifest
        /// </summary>
        private async Task<UpdateInfo?> GetUpdateInfoFromXmlAsync(string xmlUrl)
        {
            try
            {
                var xmlContent = await _httpClient.GetStringAsync(xmlUrl);
                var serializer = new XmlSerializer(typeof(UpdateManifest));

                using var reader = new StringReader(xmlContent);
                var manifest = (UpdateManifest?)serializer.Deserialize(reader);

                if (manifest == null)
                {
                    return null;
                }

                return new UpdateInfo
                {
                    Version = manifest.Version,
                    DownloadUrl = manifest.DownloadUrl,
                    ReleaseNotes = manifest.ReleaseNotes,
                    ReleaseDate = manifest.ReleaseDate,
                    FileSize = manifest.FileSize,
                    Checksum = manifest.Checksum,
                    Channel = manifest.Channel,
                    IsPrerelease = manifest.IsPrerelease
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get update info from XML: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets update information from GitHub API
        /// </summary>
        private async Task<UpdateInfo?> GetUpdateInfoFromGitHubApiAsync(string owner, string repository, string channel)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{owner}/{repository}/releases";
                var response = await _httpClient.GetStringAsync(apiUrl);

                using var doc = JsonDocument.Parse(response);
                var releases = doc.RootElement.EnumerateArray();

                foreach (var release in releases)
                {
                    var isPrerelease = release.GetProperty("prerelease").GetBoolean();

                    // Filter by channel
                    if (channel == "stable" && isPrerelease)
                        continue;
                    if (channel == "beta" && !isPrerelease)
                        continue;

                    var tagName = release.GetProperty("tag_name").GetString();
                    if (string.IsNullOrEmpty(tagName))
                        continue;

                    var version = tagName.TrimStart('v'); // Remove 'v' prefix if present
                    var releaseNotes = release.GetProperty("body").GetString() ?? "";
                    var publishedAt = release.GetProperty("published_at").GetDateTime();

                    // Find the ZIP asset
                    var assets = release.GetProperty("assets").EnumerateArray();
                    foreach (var asset in assets)
                    {
                        var assetName = asset.GetProperty("name").GetString();
                        if (assetName?.EndsWith(".zip") == true)
                        {
                            var downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            var fileSize = asset.GetProperty("size").GetInt64();

                            return new UpdateInfo
                            {
                                Version = version,
                                DownloadUrl = downloadUrl ?? "",
                                ReleaseNotes = releaseNotes,
                                ReleaseDate = publishedAt,
                                FileSize = fileSize,
                                Checksum = "", // GitHub API doesn't provide checksums
                                Channel = channel,
                                IsPrerelease = isPrerelease
                            };
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get update info from GitHub API: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads an update
        /// </summary>
        public async Task<UpdateResult> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<DownloadProgress>? progress = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                StatusChanged?.Invoke(this, "Downloading update...");

                var tempPath = Path.Combine(Path.GetTempPath(), $"StainSelector-{updateInfo.Version}.zip");

                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token)) > 0)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        StatusChanged?.Invoke(this, "Download cancelled");
                        return new UpdateResult { Success = false, Message = "Download cancelled" };
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                    downloadedBytes += bytesRead;

                    var downloadProgress = new DownloadProgress
                    {
                        BytesDownloaded = downloadedBytes,
                        TotalBytes = totalBytes,
                        Status = $"Downloading... {downloadedBytes / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB"
                    };

                    progress?.Report(downloadProgress);
                    DownloadProgressChanged?.Invoke(this, downloadProgress);
                }

                StatusChanged?.Invoke(this, "Download completed");

                // Verify checksum if available
                if (!string.IsNullOrEmpty(updateInfo.Checksum))
                {
                    if (!await VerifyChecksumAsync(tempPath, updateInfo.Checksum))
                    {
                        File.Delete(tempPath);
                        return new UpdateResult { Success = false, Message = "Checksum verification failed" };
                    }
                }

                return new UpdateResult
                {
                    Success = true,
                    Message = "Download completed successfully",
                    UpdateInfo = updateInfo
                };
            }
            catch (OperationCanceledException)
            {
                StatusChanged?.Invoke(this, "Download cancelled");
                return new UpdateResult { Success = false, Message = "Download cancelled" };
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Download failed: {ex.Message}");
                return new UpdateResult { Success = false, Message = ex.Message, Exception = ex };
            }
        }

        /// <summary>
        /// Installs a downloaded update
        /// </summary>
        public async Task<UpdateResult> InstallUpdateAsync(string zipPath, UpdateInfo updateInfo)
        {
            try
            {
                StatusChanged?.Invoke(this, "Installing update...");

                // Create backup
                var backupPath = await CreateBackupAsync();
                if (string.IsNullOrEmpty(backupPath))
                {
                    return new UpdateResult { Success = false, Message = "Failed to create backup" };
                }

                // Extract update
                var extractPath = Path.Combine(Path.GetTempPath(), $"StainSelector-{updateInfo.Version}-extract");
                Directory.CreateDirectory(extractPath);

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                }
                catch (Exception ex)
                {
                    return new UpdateResult { Success = false, Message = $"Failed to extract update: {ex.Message}", Exception = ex };
                }

                // Install files
                var appPath = Application.StartupPath;
                var result = await InstallFilesAsync(extractPath, appPath);

                // Cleanup
                try
                {
                    Directory.Delete(extractPath, true);
                    File.Delete(zipPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to cleanup temporary files: {ex.Message}");
                }

                if (result.Success)
                {
                    StatusChanged?.Invoke(this, "Update installed successfully");
                    result.BackupPath = backupPath;
                    result.RequiresRestart = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Installation failed: {ex.Message}");
                return new UpdateResult { Success = false, Message = ex.Message, Exception = ex };
            }
        }

        /// <summary>
        /// Creates a backup of the current installation
        /// </summary>
        private async Task<string> CreateBackupAsync()
        {
            try
            {
                var backupPath = Path.Combine(Application.StartupPath, "backup", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                await Task.Run(() => Directory.CreateDirectory(backupPath));

                var appPath = Application.StartupPath;
                var filesToBackup = await Task.Run(() => Directory.GetFiles(appPath, "*", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith(".log") && !f.EndsWith(".tmp"))
                    .ToList());

                foreach (var file in filesToBackup)
                {
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(backupPath, fileName);
                    await Task.Run(() => File.Copy(file, destPath, true));
                }

                return backupPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create backup: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Installs files from extracted update
        /// </summary>
        private async Task<UpdateResult> InstallFilesAsync(string extractPath, string appPath)
        {
            try
            {
                var files = await Task.Run(() => Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories));

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(extractPath, file);
                    var destPath = Path.Combine(appPath, relativePath);

                    // Ensure destination directory exists
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        await Task.Run(() => Directory.CreateDirectory(destDir));
                    }

                    // Copy file
                    await Task.Run(() => File.Copy(file, destPath, true));
                }

                return new UpdateResult { Success = true, Message = "Files installed successfully" };
            }
            catch (Exception ex)
            {
                return new UpdateResult { Success = false, Message = ex.Message, Exception = ex };
            }
        }

        /// <summary>
        /// Verifies file checksum
        /// </summary>
        private async Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = await Task.Run(() => sha256.ComputeHash(stream));
                var actualChecksum = Convert.ToHexString(hash).ToLowerInvariant();

                return actualChecksum.Equals(expectedChecksum.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to verify checksum: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Compares version strings to determine if one is newer
        /// </summary>
        private bool IsNewerVersion(string newVersion, string currentVersion)
        {
            try
            {
                var newVer = new Version(newVersion);
                var currentVer = new Version(currentVersion);
                return newVer > currentVer;
            }
            catch
            {
                // If version parsing fails, assume it's newer
                return true;
            }
        }

        /// <summary>
        /// Cancels the current operation
        /// </summary>
        public void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Restarts the application
        /// </summary>
        public void RestartApplication()
        {
            try
            {
                var exePath = Application.ExecutablePath;
                Process.Start(exePath);
                Application.Exit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restart application: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
