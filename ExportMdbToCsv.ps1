param(
    [Parameter(Mandatory=$true)]
    [string]$MdbPath,

    [Parameter(Mandatory=$true)]
    [string]$OutputDir
)

# Check if mdb file exists
if (-not (Test-Path $MdbPath)) {
    Write-Error "MDB file not found: $MdbPath"
    exit 1
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Get the directory where this script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$MdbToolsDir = Join-Path $ScriptDir "mdbtools"

# Check if mdb-tools are available
$MdbTablesExe = Join-Path $MdbToolsDir "mdb-tables.exe"
if (-not (Test-Path $MdbTablesExe)) {
    Write-Error "mdb-tools not found in: $MdbToolsDir"
    exit 1
}

Write-Host "Using mdb-tools from: $MdbToolsDir"
Write-Host "Processing database: $MdbPath"
Write-Host "Output directory: $OutputDir"

try {
    # Get list of tables
    Write-Host "Getting table list..."
    $tablesOutput = & $MdbTablesExe $MdbPath 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to get tables: $tablesOutput"
        exit 1
    }

    # Split the output into individual table names
    $tables = $tablesOutput -split '\s+' | Where-Object { $_ -match '\S' } | ForEach-Object { $_.Trim() }

    Write-Host "Found tables: $($tables -join ', ')"

    # Export each table to CSV
    foreach ($table in $tables) {
        Write-Host "Exporting table: $table"

        $outputFile = Join-Path $OutputDir "$table.csv"
        $mdbExportExe = Join-Path $MdbToolsDir "mdb-export.exe"

        # Export table to CSV
        $exportOutput = & $mdbExportExe $MdbPath $table 2>&1

        if ($LASTEXITCODE -eq 0) {
            # Write to file
            $exportOutput | Out-File -FilePath $outputFile -Encoding UTF8
            Write-Host "Exported $table to $outputFile"
        } else {
            Write-Warning "Failed to export table $table`: $exportOutput"
        }
    }

    Write-Host "Export completed successfully!"

} catch {
    Write-Error "Error during export: $($_.Exception.Message)"
    exit 1
}
