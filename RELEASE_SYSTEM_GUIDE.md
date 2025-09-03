# GitHub Actions Release System Guide

This guide explains how the automated release system works for StainSelector and how to create releases.

## 🚀 How It Works

The system uses **two main workflows** that work together to create releases:

### 1. **Release Workflow** (`.github/workflows/release.yml`)
**Triggers:** When you push a version tag (e.g., `v1.0.0`)

**What it does:**
- ✅ Extracts version from the git tag
- ✅ Builds the application in Release mode
- ✅ Creates a portable ZIP package in `dist/` folder
- ✅ Includes all required files (mdbtools, WoodStain, etc.)
- ✅ Calculates SHA256 checksum for security
- ✅ **Creates a GitHub Release** with the portable package
- ✅ Updates `update.xml` with release information
- ✅ Commits the updated `update.xml` back to the repository

### 2. **Build Workflow** (`.github/workflows/build.yml`)
**Triggers:** On pushes to main/master branches and version tags

**What it does:**
- ✅ Builds and packages the application
- ✅ Creates portable ZIP packages
- ✅ **Also creates GitHub releases** when version tags are pushed
- ✅ Uploads build artifacts

## 📦 What Gets Released

Each release includes:
- **Portable ZIP Package**: `StainSelector-{version}-portable.zip`
- **Complete Application**: All executables and dependencies
- **Required Data**: MDB tools, WoodStain data, exported CSV files
- **Update Configuration**: `update.xml` for auto-update system
- **README**: Installation and usage instructions
- **Checksum**: SHA256 hash for verification

## 🎯 How to Create a Release

### Method 1: Using update-version.ps1 (Recommended)
```powershell
# Create a complete release (updates files, builds package, creates tag, pushes to GitHub)
.\update-version.ps1 -NewVersion "1.0.0"
```

### Method 2: Manual Git Commands
```bash
# 1. Update version in StainSelector.csproj
# Edit the <Version>1.0.0</Version> line

# 2. Commit changes
git add StainSelector.csproj update.xml
git commit -m "Bump version to 1.0.0"

# 3. Create and push tag
git tag v1.0.0
git push origin main
git push origin v1.0.0
```

### Method 3: Using build-package.ps1 (Local Build Only)
```powershell
# Build package locally without creating GitHub release
.\build-package.ps1 -Clean
```

## 🔍 What Happens After You Push a Tag

1. **GitHub Actions triggers** the release workflow
2. **Builds the application** using .NET 9.0
3. **Creates portable package** with all dependencies
4. **Generates GitHub release** with:
   - Release title: "Release 1.0.0"
   - Release notes with installation instructions
   - Download link to the portable ZIP
   - SHA256 checksum for verification
5. **Updates update.xml** with new version info
6. **Commits update.xml** back to the repository

## 📋 Release Checklist

Before creating a release:

- [ ] **Test the application** locally
- [ ] **Update version** in `StainSelector.csproj`
- [ ] **Test the build** using `.\build-package.ps1 -Clean`
- [ ] **Verify all files** are included in the package
- [ ] **Commit all changes** to the repository
- [ ] **Create and push** the version tag

## 🎨 Release Features

### Automatic Features:
- ✅ **Version extraction** from git tags
- ✅ **Package creation** with all dependencies
- ✅ **Checksum calculation** for security
- ✅ **Release notes generation** with installation instructions
- ✅ **Pre-release detection** (alpha, beta, rc tags)
- ✅ **Update manifest** automatic updates

### Release Notes Include:
- Version information
- Download instructions
- System requirements
- Installation steps
- Checksum for verification
- Links to release details

## 🔧 Troubleshooting

### Release Not Created?
1. Check the **Actions tab** in GitHub for workflow logs
2. Ensure the tag follows format: `v1.0.0` (not `1.0.0`)
3. Verify the workflow files are in `.github/workflows/`
4. Check that `GITHUB_TOKEN` has release permissions

### Package Issues?
1. Test locally: `.\build-package.ps1 -Clean`
2. Check that all required files exist in the repository
3. Verify the application runs from the extracted package

### Update System Not Working?
1. Check that `update.xml` was updated with the new version
2. Verify the download URL points to the correct release
3. Ensure the checksum matches the released package

## 📊 Release History

You can view all releases in the GitHub repository:
- Go to **Releases** section in your GitHub repository
- Each release shows:
  - Version number
  - Release date
  - Download count
  - Release notes
  - Portable package download

## 🚀 Quick Start

To create your first release:

1. **Test locally:**
   ```powershell
   .\build-package.ps1 -Clean
   ```

2. **Create release:**
   ```powershell
   .\update-version.ps1 -NewVersion "1.0.0"
   ```

3. **Monitor progress:**
   - Check the **Actions** tab in GitHub
   - Wait for the workflow to complete
   - Check the **Releases** section for your new release

## 📝 Notes

- **Version tags** must start with `v` (e.g., `v1.0.0`)
- **Pre-release tags** are automatically detected (e.g., `v1.0.0-beta`)
- **Release notes** are automatically generated
- **Checksums** are calculated for security verification
- **Update system** is automatically configured for new releases

The system is designed to be **fully automated** - once you push a version tag, everything else happens automatically! 🎉
