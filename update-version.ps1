# StainSelector Version Update and Release Script
# This script updates version numbers, builds portable package, and triggers auto-update via GitHub release
#
# Parameters:
#   NewVersion        - New version number (e.g., "1.0.2")
#   CommitMessage     - Git commit message (default: "Version bump to {version}")
#   SkipGitPush       - Skip pushing to GitHub (default: false)
#   Force             - Force update even with uncommitted changes (default: false)
#   SkipPortableBuild - Skip building portable package (default: false)
#
# Usage:
#   .\update-version.ps1 -NewVersion "1.0.2"
#   .\update-version.ps1 -NewVersion "1.0.2" -SkipPortableBuild
#   .\update-version.ps1 -NewVersion "1.0.2" -SkipGitPush

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion,

    [string]$CommitMessage = "Version bump to $NewVersion",

    [switch]$SkipGitPush,

    [switch]$Force,

    [switch]$SkipPortableBuild
)

# Function to validate version format (e.g., "1.0.2")
function Test-VersionFormat {
    param([string]$Version)
    return $Version -match '^\d+\.\d+\.\d+$'
}

# Function to update XML file
function Update-XmlVersion {
    param(
        [string]$FilePath,
        [string]$Version
    )

    try {
        $xml = [xml](Get-Content $FilePath)

        # Update version
        $xml.update.version = $Version

        # Get repository info from git remote
        $remoteUrl = git remote get-url origin
        if ($remoteUrl -match "github\.com[:/]([^/]+)/([^/]+?)(?:\.git)?$") {
            $owner = $matches[1]
            $repo = $matches[2]
            $repository = "$owner/$repo"
        } else {
            $repository = "JuicyJerry/StainWareHack"  # Default fallback
        }

        # Update download URL
        $xml.update.downloadUrl = "https://github.com/$repository/releases/download/v$Version/StainSelector-$Version-portable.zip"

        # Update release notes URL
        $xml.update.releaseNotes = "https://github.com/$repository/releases/tag/v$Version"

        # Update date
        $xml.update.date = (Get-Date).ToString("yyyy-MM-dd")

        $xml.Save($FilePath)
        Write-Host "[OK] Updated $FilePath" -ForegroundColor Green
    }
    catch {
        Write-Error "[ERROR] Failed to update ${FilePath}: $($_.Exception.Message)"
        return $false
    }
    return $true
}

# Function to update .csproj file
function Update-CsprojVersion {
    param(
        [string]$FilePath,
        [string]$Version
    )

    try {
        $content = Get-Content $FilePath -Raw

        # Update Version
        $content = $content -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>"

        # Update AssemblyVersion and FileVersion (keep as 3-part version)
        $content = $content -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>"
        $content = $content -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$Version</FileVersion>"

        Set-Content $FilePath $content -NoNewline
        Write-Host "[OK] Updated $FilePath" -ForegroundColor Green
    }
    catch {
        Write-Error "[ERROR] Failed to update ${FilePath}: $($_.Exception.Message)"
        return $false
    }
    return $true
}

# Function to build package
function Build-Package {
    param([string]$Version)

    Write-Host "`n[INFO] Building portable package..." -ForegroundColor Cyan

    try {
        # Create dist directory if it doesn't exist
        $distDir = Join-Path $PSScriptRoot "dist"
        if (-not (Test-Path $distDir)) {
            New-Item -ItemType Directory -Path $distDir -Force | Out-Null
        }

        # Build the application
        Write-Host "[INFO] Building application..." -ForegroundColor Cyan
        dotnet build --configuration Release --no-restore

        if ($LASTEXITCODE -ne 0) {
            Write-Error "[ERROR] Build failed"
            return $false
        }

        # Create portable package
        $outputDir = Join-Path $PSScriptRoot "bin\Release\net9.0-windows"
        $portableDir = Join-Path $distDir "StainSelector-$Version-portable"
        $zipPath = Join-Path $distDir "StainSelector-$Version-portable.zip"

        # Remove existing portable directory and zip
        if (Test-Path $portableDir) {
            Remove-Item $portableDir -Recurse -Force
        }
        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }

        # Copy files to portable directory
        New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
        Copy-Item "$outputDir\*" $portableDir -Recurse -Force

        # Create portable zip
        Write-Host "[INFO] Creating portable zip..." -ForegroundColor Cyan
        Compress-Archive -Path "$portableDir\*" -DestinationPath $zipPath -Force

        # Clean up portable directory
        Remove-Item $portableDir -Recurse -Force

        Write-Host "[OK] Portable package built successfully: StainSelector-$Version-portable.zip" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "[ERROR] Failed to build portable package: $($_.Exception.Message)"
        return $false
    }
}

# Validate inputs
if (-not (Test-VersionFormat $NewVersion)) {
    Write-Error "[ERROR] Invalid version format. Use format like '1.0.2'"
    exit 1
}

Write-Host "=== StainSelector Version Update Script ===" -ForegroundColor Cyan
Write-Host "New Version: $NewVersion" -ForegroundColor Yellow

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Error "[ERROR] Not in a git repository"
    exit 1
}

# Check for uncommitted changes
$status = git status --porcelain
if ($status -and -not $Force) {
    Write-Warning "[WARNING] You have uncommitted changes:"
    git status --short
    $choice = Read-Host "Continue anyway? (y/N)"
    if ($choice -ne 'y' -and $choice -ne 'Y') {
        Write-Host "[CANCELLED] Aborted" -ForegroundColor Red
        exit 1
    }
}

# Update version files
Write-Host "`n[INFO] Updating version files..." -ForegroundColor Cyan

$success = $true

# Update StainSelector.csproj
if (-not (Update-CsprojVersion "StainSelector.csproj" $NewVersion)) {
    $success = $false
}

# Update update.xml
if (-not (Update-XmlVersion "update.xml" $NewVersion)) {
    $success = $false
}

if (-not $success) {
    Write-Error "[ERROR] Failed to update version files"
    exit 1
}

# Build portable package (unless skipped)
if (-not $SkipPortableBuild) {
    if (-not (Build-Package $NewVersion)) {
        Write-Error "[ERROR] Failed to build portable package"
        exit 1
    }
} else {
    Write-Host "`n[INFO] Skipping portable build (use -SkipPortableBuild:`$false to build)" -ForegroundColor Yellow
}

# Git operations
Write-Host "`n[INFO] Git operations..." -ForegroundColor Cyan

try {
    # Add changed files
    git add StainSelector.csproj update.xml

    # Add the new portable zip file if it was built
    if (-not $SkipPortableBuild) {
        $expectedZipName = "StainSelector-$NewVersion-portable.zip"
        $zipDir = Join-Path $PSScriptRoot "dist"
        $zipPath = Join-Path $zipDir $expectedZipName
        if (Test-Path $zipPath) {
            git add $zipPath
            Write-Host "[OK] Staged portable zip file: $expectedZipName" -ForegroundColor Green
        } else {
            Write-Warning "[WARNING] Portable zip file not found: $expectedZipName"
        }
    }

    Write-Host "[OK] Staged version files" -ForegroundColor Green

    # Commit changes
    git commit -m $CommitMessage
    Write-Host "[OK] Committed changes: $CommitMessage" -ForegroundColor Green

    # Create and push tag
    $tagName = "v$NewVersion"
    git tag $tagName
    Write-Host "[OK] Created tag: $tagName" -ForegroundColor Green

    if (-not $SkipGitPush) {
        Write-Host "[INFO] Pushing to GitHub..." -ForegroundColor Cyan

        # Push commits
        git push origin main
        Write-Host "[OK] Pushed commits to main branch" -ForegroundColor Green

        # Push tag (this triggers the GitHub Action release workflow)
        git push origin $tagName
        Write-Host "[OK] Pushed tag: $tagName" -ForegroundColor Green

        Write-Host "`n[SUCCESS] Release workflow should start automatically." -ForegroundColor Green
        Write-Host "[INFO] Check progress at: https://github.com/JuicyJerry/StainWareHack/actions" -ForegroundColor Cyan
        Write-Host "[INFO] Release will be available at: https://github.com/JuicyJerry/StainWareHack/releases/tag/$tagName" -ForegroundColor Cyan
    } else {
        Write-Host "`n[INFO] Git push skipped (use -SkipGitPush:`$false to push)" -ForegroundColor Yellow
        Write-Host "[INFO] To manually push: git push origin main; git push origin $tagName" -ForegroundColor Cyan
    }
}
catch {
    Write-Error "[ERROR] Git operation failed: $($_.Exception.Message)"
    exit 1
}

Write-Host "`n[SUCCESS] Version update complete!" -ForegroundColor Green
Write-Host "[INFO] Updated files:" -ForegroundColor Cyan
Write-Host "   - StainSelector.csproj (Version: $NewVersion, AssemblyVersion: $NewVersion)" -ForegroundColor White
Write-Host "   - update.xml (Version: $NewVersion)" -ForegroundColor White

if (-not $SkipPortableBuild) {
    Write-Host "   - StainSelector-$NewVersion-portable.zip (Portable package)" -ForegroundColor White
}

if (-not $SkipGitPush) {
    Write-Host "`n[INFO] The GitHub Actions workflow will:" -ForegroundColor Cyan
    Write-Host "   1. Build the application" -ForegroundColor White
    Write-Host "   2. Create a GitHub release" -ForegroundColor White
    Write-Host "   3. Upload StainSelector-v$NewVersion.zip" -ForegroundColor White
    Write-Host "   4. Update the auto-updater XML" -ForegroundColor White
    Write-Host "   5. Trigger auto-updates for existing users" -ForegroundColor White
}

Write-Host "`n[INFO] Script completed successfully with:" -ForegroundColor Cyan
Write-Host "   - Version files updated" -ForegroundColor White
if (-not $SkipPortableBuild) {
    Write-Host "   - Portable package built and committed" -ForegroundColor White
}
Write-Host "   - Git commit and tag created" -ForegroundColor White
if (-not $SkipGitPush) {
    Write-Host "   - Changes pushed to GitHub" -ForegroundColor White
}
