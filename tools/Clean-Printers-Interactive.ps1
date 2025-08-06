<#
.SYNOPSIS
  Interactive cleanup so all apps see the same current printers.

.DESCRIPTION
  - Prompts before removing each printer, unused driver, and stale registry entry.
  - Creates backups (registry exports + CSV inventories).
  - Optional preview (no changes).
  - Can restart the Print Spooler and toggle default printer to force refresh.

.PARAMETER Preview
  Show what would be done, but perform no changes.

.PARAMETER IncludeDrivers
  Also prompt to remove printer drivers that are not used by any remaining printers.

.PARAMETER IncludeRegistry
  Also prompt to remove stale entries from:
    HKCU\Software\Microsoft\Windows NT\CurrentVersion\Devices
    HKCU\Printers\Connections

.PARAMETER RestartSpooler
  Prompt to restart the Print Spooler at the end.

.EXAMPLE
  .\Clean-Printers-Interactive.ps1 -IncludeDrivers -IncludeRegistry -RestartSpooler

.NOTES
  Run PowerShell as Administrator.
#>

[CmdletBinding()]
param(
  [switch]$Preview,
  [switch]$IncludeDrivers,
  [switch]$IncludeRegistry,
  [switch]$RestartSpooler
)

function Test-Admin {
  $wi = [Security.Principal.WindowsIdentity]::GetCurrent()
  $wp = New-Object Security.Principal.WindowsPrincipal($wi)
  return $wp.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Ask-Choice {
  param([string]$Message)
  while ($true) {
    $resp = Read-Host "$Message [y]es/[n]o/[a]ll/[q]uit"
    switch ($resp.ToLower()) {
      'y' { return 'Yes' }
      'n' { return 'No' }
      'a' { return 'All' }
      'q' { return 'Quit' }
      default { Write-Host "Please enter y, n, a, or q." -ForegroundColor Yellow }
    }
  }
}

function New-Backup {
  $ts = Get-Date -Format 'yyyyMMdd-HHmmss'
  $folder = Join-Path $env:TEMP "PrinterCleanup-$ts"
  New-Item -ItemType Directory -Path $folder -ErrorAction SilentlyContinue | Out-Null

  try { reg export "HKCU\Software\Microsoft\Windows NT\CurrentVersion\Devices" (Join-Path $folder "HKCU-Devices.reg") /y | Out-Null } catch {}
  try { reg export "HKCU\Printers\Connections" (Join-Path $folder "HKCU-Printers-Connections.reg") /y | Out-Null } catch {}

  try { Get-Printer | Export-Csv (Join-Path $folder "Printers.csv") -NoTypeInformation -Encoding UTF8 } catch {}
  try { Get-PrinterDriver | Export-Csv (Join-Path $folder "Drivers.csv") -NoTypeInformation -Encoding UTF8 } catch {}

  Write-Host "Backups saved to: $folder" -ForegroundColor Cyan
  return $folder
}

function Remove-PrintersInteractive {
  param([switch]$Preview)

  $printers = @(Get-Printer | Sort-Object Name)
  if (-not $printers) {
    Write-Host "No printers found." -ForegroundColor Yellow
    return
  }

  Write-Host "`n=== Printers (to review) ===" -ForegroundColor Cyan
  $printers | ForEach-Object {
    "{0}  Type:{1}  Driver:{2}  Port:{3}  Default:{4}  WorkOffline:{5}  Status:{6}" -f $_.Name, $_.Type, $_.DriverName, $_.PortName, $_.Default, $_.WorkOffline, $_.PrinterStatus
  }

  $applyAll = $false
  foreach ($p in $printers) {
    if (-not $applyAll) {
      $choice = Ask-Choice -Message ("Remove printer '{0}' (Type:{1}, Driver:{2}, Port:{3})?" -f $p.Name, $p.Type, $p.DriverName, $p.PortName)
      if ($choice -eq 'Quit') { break }
      if ($choice -eq 'All') { $applyAll = $true }
      elseif ($choice -eq 'No') { continue }
    }

    if ($Preview) {
      Write-Host "[Preview] Would remove printer: $($p.Name)" -ForegroundColor DarkCyan
    } else {
      try {
        Remove-Printer -Name $p.Name -ErrorAction Stop
        Write-Host "Removed printer: $($p.Name)" -ForegroundColor Green
      } catch {
        Write-Warning "Failed to remove printer '$($p.Name)': $($_.Exception.Message)"
      }
    }
  }
}

function Remove-UnusedDriversInteractive {
  param([switch]$Preview)

  $printers = @(Get-Printer)
  $driversUsed = @()
  if ($printers) { $driversUsed = $printers | Select-Object -ExpandProperty DriverName -Unique }

  $drivers = @(Get-PrinterDriver | Sort-Object Name)
  if (-not $drivers) {
    Write-Host "No printer drivers found." -ForegroundColor Yellow
    return
  }

  Write-Host "`n=== Drivers (candidates for removal) ===" -ForegroundColor Cyan
  foreach ($d in $drivers) {
    $inUse = $driversUsed -contains $d.Name
    "{0}  Version:{1}  Environment:{2}  InUse:{3}" -f $d.Name, $d.DriverVersion, $d.MfgName, $inUse
  }

  $applyAll = $false
  foreach ($d in $drivers) {
    $inUse = $driversUsed -contains $d.Name
    if ($inUse) { continue } # Skip drivers still in use

    if (-not $applyAll) {
      $choice = Ask-Choice -Message ("Remove UNUSED driver '{0}' (Version:{1}, Env:{2})?" -f $d.Name, $d.DriverVersion, $d.MfgName)
      if ($choice -eq 'Quit') { break }
      if ($choice -eq 'All') { $applyAll = $true }
      elseif ($choice -eq 'No') { continue }
    }

    if ($Preview) {
      Write-Host "[Preview] Would remove driver: $($d.Name)" -ForegroundColor DarkCyan
    } else {
      try {
        Remove-PrinterDriver -Name $d.Name -ErrorAction Stop
        Write-Host "Removed driver: $($d.Name)" -ForegroundColor Green
      } catch {
        Write-Warning "Failed to remove driver '$($d.Name)': $($_.Exception.Message)"
      }
    }
  }
}

function Remove-StaleRegistryEntriesInteractive {
  param([switch]$Preview)

  $printerNames = @()
  try { $printerNames = (Get-Printer | Select-Object -ExpandProperty Name) } catch {}

  # HKCU\Software\Microsoft\Windows NT\CurrentVersion\Devices (values: printer names)
  $devicesPath = "HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Devices"
  if (Test-Path $devicesPath) {
    Write-Host "`n=== HKCU Devices values ===" -ForegroundColor Cyan
    $applyAll = $false
    $propsToSkip = "PSPath","PSParentPath","PSChildName","PSDrive","PSProvider"
    $item = Get-ItemProperty -Path $devicesPath
    foreach ($prop in $item.PSObject.Properties) {
      if ($propsToSkip -contains $prop.Name) { continue }
      $name = $prop.Name
      $isStale = -not ($printerNames -contains $name)
      Write-Host ("{0}  Value:{1}  Stale:{2}" -f $name, $prop.Value, $isStale)

      if ($isStale) {
        if (-not $applyAll) {
          $choice = Ask-Choice -Message ("Remove stale Devices entry '{0}'?" -f $name)
          if ($choice -eq 'Quit') { return }
          if ($choice -eq 'All') { $applyAll = $true }
          elseif ($choice -eq 'No') { continue }
        }
        if ($Preview) {
          Write-Host "[Preview] Would remove Devices value: $name" -ForegroundColor DarkCyan
        } else {
          try {
            Remove-ItemProperty -Path $devicesPath -Name $name -ErrorAction Stop
            Write-Host "Removed Devices value: $name" -ForegroundColor Green
          } catch {
            Write-Warning "Failed to remove Devices value '$name': $($_.Exception.Message)"
          }
        }
      }
    }
  }

  # HKCU\Printers\Connections (keys for network connections)
  $connPath = "HKCU:\Printers\Connections"
  if (Test-Path $connPath) {
    Write-Host "`n=== HKCU Printers\\Connections keys ===" -ForegroundColor Cyan
    $applyAll = $false
    Get-ChildItem -Path $connPath -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.PSIsContainer } | ForEach-Object {
      $keyPath = $_.PSPath
      # Build a plausible connection name from key structure (best-effort)
      $relative = $keyPath -replace '.*Printers\\Connections\\', ''
      $pretty = $relative -replace '\\', '\\'
      $exists = $false
      foreach ($p in $printerNames) {
        if ($p -like "\\*") {
          if ($p -eq $pretty) { $exists = $true; break }
        }
      }
      $isStale = -not $exists

      Write-Host ("{0}  ExistsInPrintersList:{1}" -f $pretty, (-not $isStale))
      if ($isStale) {
        if (-not $applyAll) {
          $choice = Ask-Choice -Message ("Remove stale Connections key '{0}'?" -f $pretty)
          if ($choice -eq 'Quit') { return }
          if ($choice -eq 'All') { $applyAll = $true }
          elseif ($choice -eq 'No') { return }
        }
        if ($Preview) {
          Write-Host "[Preview] Would remove key: $pretty" -ForegroundColor DarkCyan
        } else {
          try {
            Remove-Item -Path $keyPath -Recurse -Force -ErrorAction Stop
            Write-Host "Removed key: $pretty" -ForegroundColor Green
          } catch {
            Write-Warning "Failed to remove key '$pretty': $($_.Exception.Message)"
          }
        }
      }
    }
  }
}

function Toggle-DefaultPrinterRefresh {
  $printers = @(Get-Printer)
  if ($printers.Count -lt 2) { return }

  $current = $printers | Where-Object { $_.Default } | Select-Object -First 1
  $other = $printers | Where-Object { -not $_.Default } | Select-Object -First 1
  if (-not $current -or -not $other) { return }

  Write-Host "`nToggling default printer to force app refresh..." -ForegroundColor Cyan
  try {
    Set-Printer -Name $other.Name -IsDefault $true
    Start-Sleep -Seconds 1
    Set-Printer -Name $current.Name -IsDefault $true
    Write-Host "Default printer refresh complete." -ForegroundColor Green
  } catch {
    Write-Warning "Default printer toggle failed: $($_.Exception.Message)"
  }
}

# --- Main ---
if (-not (Test-Admin)) {
  Write-Warning "Please run this script in an elevated PowerShell (Run as Administrator)."
  return
}

$backupFolder = New-Backup

Write-Host "`nStarting interactive cleanup..." -ForegroundColor Cyan
Remove-PrintersInteractive -Preview:$Preview

if ($IncludeDrivers) {
  Remove-UnusedDriversInteractive -Preview:$Preview
}

if ($IncludeRegistry) {
  Remove-StaleRegistryEntriesInteractive -Preview:$Preview
}

Toggle-DefaultPrinterRefresh

if ($RestartSpooler) {
  $ch = Ask-Choice -Message "Restart the Print Spooler service now?"
  if ($ch -eq 'Yes' -or $ch -eq 'All') {
    if ($Preview) {
      Write-Host "[Preview] Would restart Spooler" -ForegroundColor DarkCyan
    } else {
      try {
        Restart-Service -Name Spooler -Force -ErrorAction Stop
        Write-Host "Print Spooler restarted." -ForegroundColor Green
      } catch {
        Write-Warning "Failed to restart Spooler: $($_.Exception.Message)"
      }
    }
  }
}

Write-Host "`nDone. Backups are in: $backupFolder" -ForegroundColor Cyan
