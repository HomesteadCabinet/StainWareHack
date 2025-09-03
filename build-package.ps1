# Build and Package Script for StainSelector
# This script creates a portable package similar to what GitHub Actions does

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "dist",
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

# Get project name and version
$projectName = "StainSelector"
$projectFile = "$projectName.csproj"

if (-not (Test-Path $projectFile)) {
    Write-Error "Project file $projectFile not found!"
    exit 1
}

# Get version from project file
try {
    $version = (Select-Xml -Path $projectFile -XPath "//PropertyGroup/Version").Node.InnerText
    if (-not $version) {
        $version = "1.0.0"
        Write-Warning "Version not found in project file, using default: $version"
    }
} catch {
    $version = "1.0.0"
    Write-Warning "Could not read version from project file, using default: $version"
}

Write-Host "Building $projectName version $version" -ForegroundColor Green

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force }
    if (Test-Path "publish") { Remove-Item -Path "publish" -Recurse -Force }
    if (Test-Path $OutputDir) { Remove-Item -Path $OutputDir -Recurse -Force }
}

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore dependencies"
    exit 1
}

# Build application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Publish application
Write-Host "Publishing application..." -ForegroundColor Yellow
dotnet publish --configuration $Configuration --no-build --output ./publish --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed"
    exit 1
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir
}

# Create portable package
$packageName = "$projectName-$version-portable"
$packagePath = "$OutputDir/$packageName"

Write-Host "Creating portable package..." -ForegroundColor Yellow

# Create package directory
if (Test-Path $packagePath) {
    Remove-Item -Path $packagePath -Recurse -Force
}
New-Item -ItemType Directory -Path $packagePath

# Copy published files
Write-Host "Copying application files..." -ForegroundColor Yellow
Copy-Item -Path "publish/*" -Destination $packagePath -Recurse

# Copy additional required files
$additionalFiles = @("mdbtools", "WoodStain", "exported_csv", "update.xml")
foreach ($file in $additionalFiles) {
    if (Test-Path $file) {
        Write-Host "Copying $file..." -ForegroundColor Yellow
        if ((Get-Item $file) -is [System.IO.DirectoryInfo]) {
            Copy-Item -Path $file -Destination $packagePath -Recurse
        } else {
            Copy-Item -Path $file -Destination $packagePath
        }
    }
}

# Create README for portable package
$readmeContent = @"
# $projectName Portable Package

Version: $version
Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Build Configuration: $Configuration

## Installation
1. Extract this ZIP file to your desired location
2. Run $projectName.exe

## Requirements
- Windows 10/11
- .NET 9.0 Runtime (if not self-contained)

## Files Included
- Main application executable
- Required dependencies
- MDB tools for database operations
- Wood stain data and formulas
- Update configuration

## Support
For issues and updates, visit the project repository.

## Build Information
- Built on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
- Configuration: $Configuration
- .NET Version: 9.0
- Self-contained: No
"@

$readmeContent | Out-File -FilePath "$packagePath/README.txt" -Encoding UTF8

# Create ZIP package
$zipPath = "$OutputDir/$packageName.zip"
Write-Host "Creating ZIP package..." -ForegroundColor Yellow
Compress-Archive -Path "$packagePath/*" -DestinationPath $zipPath -Force

# Clean up temporary directory
Remove-Item -Path $packagePath -Recurse -Force

# Display results
Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Package created: $zipPath" -ForegroundColor Green
$packageSize = (Get-Item $zipPath).Length
Write-Host "Package size: $([math]::Round($packageSize / 1MB, 2)) MB" -ForegroundColor Green

# List contents of the package
Write-Host "`nPackage contents:" -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
$zip.Entries | ForEach-Object { Write-Host "  $($_.FullName)" }
$zip.Dispose()

Write-Host "`nTo test the package:" -ForegroundColor Yellow
Write-Host "1. Extract $zipPath to a temporary folder" -ForegroundColor Yellow
Write-Host "2. Run $projectName.exe from the extracted folder" -ForegroundColor Yellow
