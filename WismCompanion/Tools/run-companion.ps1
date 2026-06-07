<#
.SYNOPSIS
    Launch the built WismCompanion standalone exe.

.PARAMETER ExePath
    Path to WismCompanion.exe. Defaults to <project>\Build\Win64\WismCompanion.exe.

.PARAMETER Build
    Build first (via build-companion.ps1) if the exe is missing or -Build is passed.

.EXAMPLE
    .\run-companion.ps1
.EXAMPLE
    .\run-companion.ps1 -Build
#>
[CmdletBinding()]
param(
    [string]$ExePath,
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
if (-not $ExePath) { $ExePath = Join-Path $projectPath 'Build\Win64\WismCompanion.exe' }

if ($Build -or -not (Test-Path $ExePath)) {
    if (-not (Test-Path $ExePath)) { Write-Host "No build found - building first..." -ForegroundColor Yellow }
    & (Join-Path $PSScriptRoot 'build-companion.ps1')
}

if (-not (Test-Path $ExePath)) {
    Write-Host "Companion exe not found: $ExePath" -ForegroundColor Red
    Write-Host "Build it with: .\build-companion.ps1   (or WISM > Build Windows Player in the Editor)"
    exit 1
}

Write-Host "Launching $ExePath"
Start-Process -FilePath $ExePath
