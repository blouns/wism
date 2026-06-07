<#
.SYNOPSIS
    Headless build of the WismCompanion standalone Windows player.

.DESCRIPTION
    Runs Unity in batchmode against the WismCompanion project and invokes
    WismCompanion.EditorTools.CompanionBuild.BuildWindows. Output goes to <project>\Build\Win64
    (git-ignored) by default.

    The Unity Editor must NOT have this project open (batchmode can't build a locked project).
    For an Editor-open build, use the menu: WISM > Build Windows Player.

.PARAMETER UnityPath
    Full path to Unity.exe. Falls back to $env:UNITY_EXE, then the Hub editor matching
    ProjectVersion.txt, then the newest installed Hub editor.

.PARAMETER OutputDir
    Build output directory. Defaults to <project>\Build\Win64.

.EXAMPLE
    .\build-companion.ps1
.EXAMPLE
    .\build-companion.ps1 -UnityPath 'D:\Unity\6000.0.34f1\Editor\Unity.exe'
#>
[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
if (-not $OutputDir) { $OutputDir = Join-Path $projectPath 'Build\Win64' }

function Resolve-Unity {
    param([string]$Explicit, [string]$ProjectPath)

    if ($Explicit) {
        if (Test-Path $Explicit) { return $Explicit }
        throw "Unity not found at -UnityPath '$Explicit'."
    }
    if ($env:UNITY_EXE -and (Test-Path $env:UNITY_EXE)) { return $env:UNITY_EXE }

    $hubRoots = @(
        (Join-Path $env:ProgramFiles 'Unity\Hub\Editor'),
        (Join-Path ${env:ProgramFiles(x86)} 'Unity\Hub\Editor')
    ) | Where-Object { $_ -and (Test-Path $_) }

    $versionFile = Join-Path $ProjectPath 'ProjectSettings/ProjectVersion.txt'
    if (Test-Path $versionFile) {
        $line = Get-Content $versionFile | Where-Object { $_ -like 'm_EditorVersion:*' } | Select-Object -First 1
        if ($line) {
            $version = ($line -replace 'm_EditorVersion:\s*', '').Trim()
            foreach ($root in $hubRoots) {
                $candidate = Join-Path $root "$version\Editor\Unity.exe"
                if (Test-Path $candidate) { return $candidate }
            }
        }
    }

    foreach ($root in $hubRoots) {
        $newest = Get-ChildItem $root -Directory | Sort-Object Name -Descending | Select-Object -First 1
        if ($newest) {
            $candidate = Join-Path $newest.FullName 'Editor\Unity.exe'
            if (Test-Path $candidate) { return $candidate }
        }
    }

    throw "Could not locate Unity. Pass -UnityPath or set `$env:UNITY_EXE."
}

$unity = Resolve-Unity -Explicit $UnityPath -ProjectPath $projectPath
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
# Keep the log out of the player output folder so it isn't disturbed when the build writes there.
$logFile = Join-Path (Split-Path $OutputDir -Parent) 'build.log'

Write-Host "Unity:   $unity"
Write-Host "Project: $projectPath"
Write-Host "Output:  $OutputDir"
Write-Host "Log:     $logFile"

$buildStartedUtc = [DateTime]::UtcNow
$unityArgs = @(
    '-batchmode', '-quit', '-nographics',
    '-projectPath', $projectPath,
    '-executeMethod', 'WismCompanion.EditorTools.CompanionBuild.BuildWindows',
    '-logFile', $logFile,
    '-buildOutput', $OutputDir
)

& $unity @unityArgs
$code = $LASTEXITCODE
$exe = Join-Path $OutputDir 'WismCompanion.exe'

$logIndicatesSuccess = $false
$logFromThisRun = $false
if (Test-Path $logFile) {
    $logIndicatesSuccess = Select-String -Path $logFile -SimpleMatch '[WismCompanion] Build succeeded' -Quiet
    $logFromThisRun = (Get-Item $logFile).LastWriteTimeUtc -ge $buildStartedUtc.AddSeconds(-2)
}

$exeExists = Test-Path $exe

if ($code -ne 0) {
    if ($logIndicatesSuccess -and $logFromThisRun -and $exeExists) {
        Write-Host "Unity exited $code, but build output and log indicate success. Continuing." -ForegroundColor Yellow
    }
    else {
        Write-Host "Build FAILED (exit $code). See $logFile" -ForegroundColor Red
        if (Test-Path $logFile) {
            Write-Host "---- build.log tail ----" -ForegroundColor DarkYellow
            Get-Content -Path $logFile -Tail 40 | ForEach-Object { Write-Host $_ }
            Write-Host "------------------------" -ForegroundColor DarkYellow
        }
        exit $code
    }
}

if (-not $exeExists) {
    Write-Host "Build reported success but exe is missing: $exe" -ForegroundColor Red
    exit 1
}

Write-Host "Build succeeded: $exe" -ForegroundColor Green
