# GitHub Actions Setup for StainSelector

This repository includes automated GitHub Actions workflows for building, testing, and releasing the StainSelector application.

## Workflows

### 1. CI Workflow (`.github/workflows/ci.yml`)
**Triggers:**
- Push to `main`, `master`, or `develop` branches
- Pull requests to `main` or `master` branches

**Actions:**
- Restores .NET dependencies
- Builds the application in Release configuration
- Runs tests (if any exist)
- Publishes the application
- Uploads build artifacts

### 2. Build and Package Workflow (`.github/workflows/build.yml`)
**Triggers:**
- Push to `main` or `master` branches
- Push of version tags (`v*`)
- Pull requests to `main` or `master` branches
- Manual workflow dispatch

**Actions:**
- Builds and publishes the application
- Creates a portable ZIP package in the `dist/` folder
- Includes all required dependencies and files
- Uploads build artifacts
- Creates GitHub releases for version tags
- Updates `update.xml` with new version information

### 3. Release Workflow (`.github/workflows/release.yml`)
**Triggers:**
- Push of version tags (`v*.*.*` or `v*.*.*-*`)

**Actions:**
- Extracts version from git tag
- Builds and packages the application
- Calculates SHA256 checksum
- Updates `update.xml` with release information
- Creates GitHub release with:
  - Release notes
  - Portable package download
  - Checksum verification
  - Automatic pre-release detection

## Package Contents

The generated portable package includes:
- Main application executable (`StainSelector.exe`)
- .NET runtime dependencies
- MDB tools for database operations
- Wood stain data and formulas
- Update configuration (`update.xml`)
- README with installation instructions

## Creating a Release

### Method 1: Using Git Tags (Recommended)
1. Update version in `StainSelector.csproj`:
   ```xml
   <PropertyGroup>
     <Version>1.0.0</Version>
   </PropertyGroup>
   ```

2. Commit and push changes:
   ```bash
   git add StainSelector.csproj
   git commit -m "Bump version to 1.0.0"
   git push
   ```

3. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

4. The release workflow will automatically:
   - Build the application
   - Create a portable package
   - Generate a GitHub release
   - Update `update.xml`

### Method 2: Using the update-version.ps1 Script
1. Run the version update script:
   ```powershell
   .\update-version.ps1 -Version "1.0.0"
   ```

2. This will:
   - Update version in `StainSelector.csproj`
   - Update `update.xml`
   - Create a git tag
   - Push changes and tag

## Local Development

### Building Packages Locally
Use the provided PowerShell script to build packages locally:

```powershell
# Build with default settings
.\build-package.ps1

# Build with clean
.\build-package.ps1 -Clean

# Build with specific configuration
.\build-package.ps1 -Configuration Debug
```

### Testing the Package
1. Build the package using the script above
2. Extract the generated ZIP file
3. Run `StainSelector.exe` from the extracted folder

## File Structure

```
.github/
├── workflows/
│   ├── ci.yml          # Continuous Integration
│   ├── build.yml       # Build and Package
│   └── release.yml     # Release Management
dist/                   # Generated packages
├── StainSelector-1.0.0-portable.zip
└── ...
build-package.ps1       # Local build script
update-version.ps1      # Version management script
update.xml              # Update manifest (auto-updated)
```

## Environment Variables

The workflows use these environment variables:
- `DOTNET_VERSION`: .NET SDK version (default: `9.0.x`)
- `PROJECT_NAME`: Project name (default: `StainSelector`)

## Secrets

No additional secrets are required. The workflows use the default `GITHUB_TOKEN` for:
- Creating releases
- Uploading artifacts
- Pushing updated files

## Troubleshooting

### Build Failures
1. Check the Actions tab in GitHub for detailed logs
2. Ensure all dependencies are properly referenced in `StainSelector.csproj`
3. Verify that all required files exist in the repository

### Release Issues
1. Ensure the git tag follows the format `v*.*.*`
2. Check that the version in `StainSelector.csproj` matches the tag
3. Verify that `update.xml` is properly formatted

### Package Issues
1. Test the package locally using `build-package.ps1`
2. Check that all required files are included in the package
3. Verify that the application runs correctly from the extracted package

## Customization

### Adding Files to Package
To include additional files in the portable package, modify the `additionalFiles` array in the workflows:

```powershell
$additionalFiles = @("mdbtools", "WoodStain", "exported_csv", "update.xml", "your-new-folder")
```

### Changing Package Name
Modify the `packageName` variable in the workflows:

```powershell
$packageName = "$env:PROJECT_NAME-$version-portable"
```

### Adding Build Steps
Add additional steps to the workflows as needed, such as:
- Code signing
- Additional testing
- Documentation generation
- Custom validation

## Support

For issues with the GitHub Actions setup:
1. Check the Actions tab for workflow logs
2. Review this documentation
3. Create an issue in the repository
