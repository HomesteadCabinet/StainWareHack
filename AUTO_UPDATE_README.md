# StainSelector Auto-Update System

This document describes the comprehensive portable auto-update system integrated into StainSelector, which provides seamless updates via GitHub releases without requiring administrator privileges.

## Overview

The auto-update system consists of several key components:

- **PortableUpdateManager**: Core update management functionality
- **DeployKeyManager**: SSH key management for secure GitHub access
- **PortableUpdateDialog**: User interface for update notifications
- **UpdateConfigurationService**: Configuration management
- **UpdateSettingsDialog**: Settings configuration interface

## Features

### Core Update Management
- ✅ Automatic update checks on application startup
- ✅ Manual update checks via Help menu
- ✅ Version comparison and update availability detection
- ✅ User preferences for skipped versions and update frequency
- ✅ Support for multiple update channels (stable, beta, dev)

### GitHub Integration
- ✅ GitHub API integration for release information
- ✅ Support for both public and private repositories
- ✅ Deploy key authentication for secure access
- ✅ Download of update.xml manifest files
- ✅ Graceful handling of GitHub rate limiting

### Security Features
- ✅ Deploy key management with secure file permissions
- ✅ Checksum verification for downloaded files
- ✅ Prevention of downgrade attacks
- ✅ Secure backup mechanisms
- ✅ Validation of update sources

### User Experience
- ✅ Professional update dialog with progress indicators
- ✅ Download progress with percentage and status
- ✅ Installation progress tracking
- ✅ Graceful cancellation support
- ✅ Automatic application restart after updates

## File Structure

```
StainSelector/
├── Models/
│   └── UpdateInfo.cs                 # Update data models
├── Services/
│   ├── PortableUpdateManager.cs      # Core update manager
│   ├── DeployKeyManager.cs           # SSH key management
│   └── UpdateConfigurationService.cs # Configuration service
├── Resources/
│   └── deploy_key                    # Embedded deploy key (placeholder)
├── PortableUpdateDialog.cs           # Update notification dialog
├── UpdateSettingsDialog.cs           # Settings configuration dialog
├── update.xml                        # Update manifest template
├── update-version.ps1                # Version update script
└── AUTO_UPDATE_README.md             # This documentation
```

## Configuration

### Update Configuration File
The system stores configuration in `update-config.xml` in the application directory:

```xml
<?xml version="1.0" encoding="utf-8"?>
<UpdateConfiguration>
  <GitHubOwner>JuicyJerry</GitHubOwner>
  <GitHubRepository>StainWareHack</GitHubRepository>
  <UpdateChannel>stable</UpdateChannel>
  <CheckForUpdatesOnStartup>true</CheckForUpdatesOnStartup>
  <CheckForPrereleases>false</CheckForPrereleases>
  <CheckFrequencyHours>24</CheckFrequencyHours>
  <LastUpdateCheck>2024-01-01T00:00:00Z</LastUpdateCheck>
  <SkippedVersions>
    <string>1.0.1</string>
  </SkippedVersions>
  <AutoDownloadUpdates>false</AutoDownloadUpdates>
  <AutoInstallUpdates>false</AutoInstallUpdates>
  <DeployKeyPath>deploy-key</DeployKeyPath>
  <BackupPath>backup</BackupPath>
</UpdateConfiguration>
```

### Update Manifest (update.xml)
The system supports custom update manifests for more control:

```xml
<?xml version="1.0" encoding="utf-8"?>
<item>
  <version>1.0.2</version>
  <url>https://github.com/JuicyJerry/StainWareHack/releases/download/v1.0.2/StainSelector-v1.0.2.zip</url>
  <changelog>
    <![CDATA[
    <h3>Version 1.0.2</h3>
    <ul>
      <li>Bug fixes and improvements</li>
      <li>Enhanced auto-update system</li>
    </ul>
    ]]>
  </changelog>
  <mandatory>false</mandatory>
  <mode>2</mode>
  <execute>StainSelector.exe</execute>
  <executeargs></executeargs>
  <minversion>1.0.0</minversion>
  <maxversion></maxversion>
  <hash>sha256</hash>
  <hashvalue></hashvalue>
  <targetpath></targetpath>
  <targetfilename></targetfilename>
  <checkforupdatesonstartup>true</checkforupdatesonstartup>
  <autodownload>false</autodownload>
  <autoupdate>false</autoupdate>
  <channel>stable</channel>
  <prerelease>false</prerelease>
  <filesize>0</filesize>
  <releasedate>2024-01-01T00:00:00Z</releasedate>
</item>
```

## Usage

### For End Users

1. **Automatic Updates**: The application checks for updates on startup (if enabled)
2. **Manual Check**: Use Help → Check for Updates
3. **Settings**: Use Help → Update Settings to configure update behavior
4. **Update Process**: When an update is available, a dialog will appear with options to:
   - Download and install the update
   - Skip this version
   - Remind later

### For Developers

#### Updating Version Numbers

Use the provided PowerShell script to update version numbers and create releases:

```powershell
# Update to version 1.0.2
.\update-version.ps1 -NewVersion "1.0.2"

# Update with custom commit message
.\update-version.ps1 -NewVersion "1.0.2" -CommitMessage "Bug fixes and improvements"

# Skip building portable package
.\update-version.ps1 -NewVersion "1.0.2" -SkipPortableBuild

# Skip pushing to GitHub (for testing)
.\update-version.ps1 -NewVersion "1.0.2" -SkipGitPush
```

#### Setting Up Deploy Keys

1. Generate a new SSH key:
   ```bash
   ssh-keygen -t ed25519 -f deploy_key -C "your-email@example.com"
   ```

2. Add the public key to your GitHub repository:
   - Go to repository Settings → Deploy keys
   - Add the public key content
   - Enable "Allow write access" if needed

3. Replace the placeholder in `Resources/deploy_key` with your private key

#### GitHub Repository Setup

1. **Tagged Releases**: Create releases with version tags (e.g., v1.0.2)
2. **Release Assets**: Upload ZIP files with naming convention: `StainSelector-v{version}.zip`
3. **Update Manifest**: Optionally include `update.xml` in releases for custom update information

## API Reference

### PortableUpdateManager

Main class for managing updates:

```csharp
// Check for updates
var updateInfo = await updateManager.CheckForUpdatesAsync();

// Download update
var result = await updateManager.DownloadUpdateAsync(updateInfo, progress);

// Install update
var installResult = await updateManager.InstallUpdateAsync(zipPath, updateInfo);

// Restart application
updateManager.RestartApplication();
```

### UpdateConfigurationService

Manages update configuration:

```csharp
// Check if update check is needed
bool shouldCheck = configService.ShouldCheckForUpdates();

// Skip a version
configService.SkipVersion("1.0.1");

// Update repository info
configService.UpdateRepositoryInfo("owner", "repository");

// Update auto-update settings
configService.UpdateAutoUpdateSettings(true, false);
```

### DeployKeyManager

Manages SSH deploy keys:

```csharp
// Initialize deploy key system
bool success = await deployKeyManager.InitializeDeployKeyAsync();

// Check if deploy key is configured
bool configured = deployKeyManager.IsDeployKeyConfigured();

// Get SSH command arguments
string args = deployKeyManager.GetSshCommandArgs();
```

## Security Considerations

1. **Deploy Keys**: Use dedicated deploy keys with minimal permissions
2. **Checksum Verification**: Always verify downloaded files using SHA256 checksums
3. **HTTPS Only**: All downloads use HTTPS to prevent man-in-the-middle attacks
4. **File Permissions**: Deploy keys are stored with restricted file permissions
5. **Backup Creation**: Automatic backups are created before applying updates
6. **Rollback Support**: Failed updates can be rolled back using backups

## Error Handling

The system includes comprehensive error handling for:

- Network connectivity issues
- GitHub API failures and rate limiting
- Insufficient disk space
- File permission errors
- Update validation failures
- Corrupted downloads
- Installation failures

## Troubleshooting

### Common Issues

1. **Update Check Fails**
   - Check internet connectivity
   - Verify GitHub repository settings
   - Check deploy key configuration

2. **Download Fails**
   - Verify release asset exists
   - Check file permissions
   - Ensure sufficient disk space

3. **Installation Fails**
   - Check file permissions
   - Ensure application is not running
   - Verify backup creation

### Debug Information

Enable debug logging by setting the application to debug mode. The system will log detailed information about update operations to the debug output.

## Future Enhancements

Potential improvements for future versions:

- Delta updates for smaller download sizes
- Background update downloads
- Update rollback UI
- Multiple update channels with different release schedules
- Integration with package managers
- Update notifications via system tray
- Scheduled update installations

## License

This auto-update system is part of StainSelector and follows the same license terms as the main application.
