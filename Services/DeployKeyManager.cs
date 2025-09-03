using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StainSelector.Services
{
    /// <summary>
    /// Manages SSH deploy keys for GitHub authentication
    /// </summary>
    public class DeployKeyManager
    {
        private readonly string _deployKeyPath;
        private readonly string _sshConfigPath;
        private readonly string _knownHostsPath;

        public DeployKeyManager(string deployKeyPath)
        {
            _deployKeyPath = deployKeyPath;
            _sshConfigPath = Path.Combine(Path.GetDirectoryName(deployKeyPath)!, "ssh_config");
            _knownHostsPath = Path.Combine(Path.GetDirectoryName(deployKeyPath)!, "known_hosts");
        }

        /// <summary>
        /// Initializes the deploy key system
        /// </summary>
        public async Task<bool> InitializeDeployKeyAsync()
        {
            try
            {
                // Create directory if it doesn't exist
                var keyDirectory = Path.GetDirectoryName(_deployKeyPath);
                if (!Directory.Exists(keyDirectory))
                {
                    Directory.CreateDirectory(keyDirectory!);
                }

                // Check if deploy key exists
                if (!File.Exists(_deployKeyPath))
                {
                    return await CreateDeployKeyAsync();
                }

                // Validate existing key
                return await ValidateDeployKeyAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize deploy key: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new deploy key from embedded resource or user SSH directory
        /// </summary>
        private async Task<bool> CreateDeployKeyAsync()
        {
            try
            {
                // Try to extract from embedded resource first
                if (await ExtractDeployKeyFromResourceAsync())
                {
                    return true;
                }

                // Try to copy from user SSH directory
                if (await CopyDeployKeyFromUserSshAsync())
                {
                    return true;
                }

                // Create a placeholder key file (user will need to replace with actual key)
                await CreatePlaceholderKeyAsync();
                return false; // Indicates user action required
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create deploy key: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts deploy key from embedded resource
        /// </summary>
        private async Task<bool> ExtractDeployKeyFromResourceAsync()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "StainSelector.Resources.deploy_key";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    return false; // No embedded resource found
                }

                using var reader = new StreamReader(stream);
                var keyContent = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(keyContent))
                {
                    return false;
                }

                await File.WriteAllTextAsync(_deployKeyPath, keyContent);
                SetSecureFilePermissions(_deployKeyPath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to extract deploy key from resource: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Copies deploy key from user SSH directory
        /// </summary>
        private async Task<bool> CopyDeployKeyFromUserSshAsync()
        {
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var sshDirectory = Path.Combine(userProfile, ".ssh");

                if (!Directory.Exists(sshDirectory))
                {
                    return false;
                }

                // Look for common deploy key names
                var possibleKeyNames = new[]
                {
                    "github_deploy_key",
                    "deploy_key",
                    "id_rsa_deploy",
                    "id_ed25519_deploy"
                };

                foreach (var keyName in possibleKeyNames)
                {
                    var sourceKeyPath = Path.Combine(sshDirectory, keyName);
                    if (File.Exists(sourceKeyPath))
                    {
                        var keyContent = await File.ReadAllTextAsync(sourceKeyPath);
                        await File.WriteAllTextAsync(_deployKeyPath, keyContent);
                        SetSecureFilePermissions(_deployKeyPath);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy deploy key from user SSH: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a placeholder key file with instructions
        /// </summary>
        private async Task CreatePlaceholderKeyAsync()
        {
            var placeholderContent = @"# GitHub Deploy Key
#
# This file should contain your GitHub deploy key for accessing private repositories.
#
# To set up a deploy key:
# 1. Generate a new SSH key: ssh-keygen -t ed25519 -f deploy_key -C ""your-email@example.com""
# 2. Add the public key to your GitHub repository settings
# 3. Replace this file content with your private key
#
# The key should start with -----BEGIN OPENSSH PRIVATE KEY----- or similar
# and end with -----END OPENSSH PRIVATE KEY-----
";

            await File.WriteAllTextAsync(_deployKeyPath, placeholderContent);
        }

        /// <summary>
        /// Validates the existing deploy key
        /// </summary>
        private async Task<bool> ValidateDeployKeyAsync()
        {
            try
            {
                var keyContent = await File.ReadAllTextAsync(_deployKeyPath);

                // Check if it's a placeholder
                if (keyContent.Contains("# GitHub Deploy Key") && keyContent.Contains("# This file should contain"))
                {
                    return false; // Still a placeholder
                }

                // Basic validation - check for key markers
                if (keyContent.Contains("-----BEGIN") && keyContent.Contains("-----END"))
                {
                    // Verify checksum if available
                    return await VerifyKeyChecksumAsync(keyContent);
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to validate deploy key: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifies key integrity using checksum
        /// </summary>
        private async Task<bool> VerifyKeyChecksumAsync(string keyContent)
        {
            try
            {
                // Calculate SHA256 hash of the key content
                using var sha256 = SHA256.Create();
                var keyBytes = Encoding.UTF8.GetBytes(keyContent);
                var hash = sha256.ComputeHash(keyBytes);
                var checksum = Convert.ToHexString(hash);

                // Check if checksum file exists and matches
                var checksumPath = _deployKeyPath + ".checksum";
                if (File.Exists(checksumPath))
                {
                    var storedChecksum = await File.ReadAllTextAsync(checksumPath);
                    return storedChecksum.Trim().Equals(checksum, StringComparison.OrdinalIgnoreCase);
                }

                // No checksum file, assume valid for now
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to verify key checksum: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets secure file permissions on Windows
        /// </summary>
        private void SetSecureFilePermissions(string filePath)
        {
            try
            {
                // On Windows, set file permissions to be readable only by the current user
                var fileInfo = new FileInfo(filePath);
                fileInfo.Attributes |= FileAttributes.ReadOnly;

                // Additional security could be implemented here using Windows ACLs
                // For now, we rely on the read-only attribute
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set secure file permissions: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates SSH configuration for GitHub
        /// </summary>
        public async Task CreateSshConfigAsync()
        {
            try
            {
                var sshConfig = $@"Host github.com
    HostName github.com
    User git
    IdentityFile {_deployKeyPath}
    IdentitiesOnly yes
    StrictHostKeyChecking no
    UserKnownHostsFile {_knownHostsPath}
";

                await File.WriteAllTextAsync(_sshConfigPath, sshConfig);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create SSH config: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates known hosts file for GitHub
        /// </summary>
        public async Task CreateKnownHostsAsync()
        {
            try
            {
                // GitHub's SSH key fingerprints (as of 2023)
                var knownHosts = @"github.com ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIOMqqnkVzrm0SdG6UOoqKLsabgH5C9okWi0dh2l9GKJl
github.com ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQCj7ndNxQowgcQnjshcLrqPEiiphnt+VTTvDP6mHBL9j1aNUkY4Ue1gvwnGLVlOhGeYrnZaMgRK6+PKCUXaDbC7qtbW8gIkhL7aGCsOr/C56SJMy/BCZfxd1nWzAOxSDPgVsmerOBYfNqltV9/hWCqBywINIR+5dIg6JTJ72pcEpEjcYgXkE2YEFXV1JHnsKgbLWNlhScqb2UmyRkQyytRLtL+38TGxkxCflmO+5Z8CSSNY7GidjMIZ7Q4zMjA2n1nGrlTDkzwDCsw+wqFPGQA179cnfGWOWRVruj16z6XyvxvjJwbz0wQZ75XK5tKSb7FNyeIEs4TT4jk+S4dhPeAUC5y+bDYirYgM4GC7uEnztnZyaVWQ7B381AK4Qdrwt51ZqExKbQpTUNn+EjqoTwvqNj4kqx5QUCI0ThS/YkOxJCXmPUWZbhjpCg56i+2aB6CmK2JGhn57K5mj0MNdBXA4/WnwH6XoPWJzK5Nyu2zB3munknB2xOvUWafl5BQ==
";

                await File.WriteAllTextAsync(_knownHostsPath, knownHosts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create known hosts: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the SSH command arguments for Git operations
        /// </summary>
        public string GetSshCommandArgs()
        {
            return $"-i \"{_deployKeyPath}\" -F \"{_sshConfigPath}\"";
        }

        /// <summary>
        /// Checks if deploy key is properly configured
        /// </summary>
        public bool IsDeployKeyConfigured()
        {
            return File.Exists(_deployKeyPath) &&
                   File.Exists(_sshConfigPath) &&
                   File.Exists(_knownHostsPath);
        }

        /// <summary>
        /// Cleans up deploy key files
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (File.Exists(_deployKeyPath))
                {
                    File.Delete(_deployKeyPath);
                }
                if (File.Exists(_sshConfigPath))
                {
                    File.Delete(_sshConfigPath);
                }
                if (File.Exists(_knownHostsPath))
                {
                    File.Delete(_knownHostsPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup deploy key files: {ex.Message}");
            }
        }
    }
}
