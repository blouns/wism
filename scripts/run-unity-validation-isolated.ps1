[CmdletBinding()]
param(
    [string]$UnityExe = $env:UNITY_EXE,
    [string]$WorktreePath = "",
    [string]$RunId = "",
    [string]$TestFilter = "ModSettingsUiTests",
    [string]$Profile = "classic-warlords",
    [string]$Packs = "pack-illurian-legends-flavor",
    [string]$World = "TestWorld",
    [string[]]$DirtyPath = @(
        "WismClient",
        "WismUnity/Assets",
        "WismUnity/Packages",
        "WismUnity/ProjectSettings",
        "docs",
        "scripts"
    ),
    [string[]]$ExcludeDirtyPath = @(
        ".tmp/*",
        "artifacts/*",
        "WismUnity/Assets/Plugins/WismClient/*.dll",
        "WismUnity/Assets/Plugins/WismClient/*.pdb",
        "WismUnity/Assets/RenderTextures/*"
    ),
    [switch]$NoDirtyOverlay,
    [switch]$SkipClientBuild,
    [switch]$SkipClientTests,
    [switch]$SkipProof,
    [int]$TimeoutMinutes = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $candidate = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
    $root = (& git -C $candidate rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
        throw "Could not resolve repository root from $candidate."
    }

    return [System.IO.Path]::GetFullPath($root.Trim())
}

function Resolve-UnityExe {
    param([string]$Requested)

    if (-not [string]::IsNullOrWhiteSpace($Requested) -and (Test-Path -LiteralPath $Requested)) {
        return (Resolve-Path -LiteralPath $Requested).Path
    }

    $default = "C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe"
    if (Test-Path -LiteralPath $default) {
        return $default
    }

    throw "Unity executable was not found. Pass -UnityExe or set UNITY_EXE."
}

function Invoke-Native {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [string]$LogPath
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $LogPath) | Out-Null
    "[$(Get-Date -Format o)] $Name" | Set-Content -LiteralPath $LogPath
    "WorkingDirectory: $WorkingDirectory" | Add-Content -LiteralPath $LogPath
    "Command: $FilePath $($Arguments -join ' ')" | Add-Content -LiteralPath $LogPath
    "" | Add-Content -LiteralPath $LogPath

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments *>> $LogPath
        $code = if ($null -eq $global:LASTEXITCODE) { 0 } else { $global:LASTEXITCODE }
    }
    finally {
        Pop-Location
    }

    return [ordered]@{
        name = $Name
        exitCode = $code
        logPath = $LogPath
    }
}

function Get-CurrentHead {
    param([string]$RepositoryRoot)

    $head = (& git -C $RepositoryRoot rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
        throw "Could not resolve current HEAD."
    }

    return $head.Trim()
}

function Test-RegisteredWorktree {
    param([string]$RepositoryRoot, [string]$Path)

    $target = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
    $lines = & git -C $RepositoryRoot worktree list --porcelain
    if ($LASTEXITCODE -ne 0) {
        throw "git worktree list failed."
    }

    foreach ($line in $lines) {
        if ($line.StartsWith("worktree ", [System.StringComparison]::OrdinalIgnoreCase)) {
            $listed = [System.IO.Path]::GetFullPath($line.Substring("worktree ".Length)).TrimEnd('\')
            if ([string]::Equals($listed, $target, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
}

function Initialize-ValidationWorktree {
    param([string]$RepositoryRoot, [string]$Path, [string]$Head)

    if (Test-Path -LiteralPath $Path) {
        if (-not (Test-RegisteredWorktree -RepositoryRoot $RepositoryRoot -Path $Path)) {
            throw "Refusing to reuse $Path because it is not a registered git worktree."
        }

        & git -C $Path reset --hard $Head | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "git reset failed in validation worktree."
        }

        & git -C $Path clean -fdx | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "git clean failed in validation worktree."
        }

        return
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
    & git -C $RepositoryRoot worktree add --detach $Path $Head | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "git worktree add failed for $Path."
    }
}

function Convert-StatusPath {
    param([string]$StatusLine)

    if ($StatusLine.Length -lt 4) {
        return ""
    }

    $path = $StatusLine.Substring(3)
    if ($path.Contains(" -> ")) {
        $path = ($path -split " -> ")[-1]
    }

    return $path.Trim('"')
}

function Test-AllowedDirtyPath {
    param([string]$RelativePath, [string[]]$AllowedRoots)

    $normalized = $RelativePath.Replace('\', '/')
    foreach ($root in $AllowedRoots) {
        $allowed = $root.Replace('\', '/').TrimEnd('/')
        if ($normalized.Equals($allowed, [System.StringComparison]::OrdinalIgnoreCase) -or
            $normalized.StartsWith($allowed + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Test-ExcludedDirtyPath {
    param([string]$RelativePath, [string[]]$ExcludedPatterns)

    $normalized = $RelativePath.Replace('\', '/')
    foreach ($pattern in $ExcludedPatterns) {
        $wildcard = New-Object System.Management.Automation.WildcardPattern -ArgumentList $pattern.Replace('\', '/'), ([System.Management.Automation.WildcardOptions]::IgnoreCase)
        if ($wildcard.IsMatch($normalized)) {
            return $true
        }
    }

    return $false
}

function Copy-DirtyOverlay {
    param(
        [string]$RepositoryRoot,
        [string]$ValidationRoot,
        [string[]]$AllowedRoots,
        [string[]]$ExcludedPatterns
    )

    $copied = New-Object System.Collections.Generic.List[string]
    $removed = New-Object System.Collections.Generic.List[string]
    $status = & git -C $RepositoryRoot status --porcelain=v1 -uall
    if ($LASTEXITCODE -ne 0) {
        throw "git status failed while preparing dirty overlay."
    }

    foreach ($line in $status) {
        $relative = Convert-StatusPath -StatusLine $line
        if ([string]::IsNullOrWhiteSpace($relative)) {
            continue
        }

        if (-not (Test-AllowedDirtyPath -RelativePath $relative -AllowedRoots $AllowedRoots)) {
            continue
        }

        if (Test-ExcludedDirtyPath -RelativePath $relative -ExcludedPatterns $ExcludedPatterns) {
            continue
        }

        $source = Join-Path $RepositoryRoot $relative
        $target = Join-Path $ValidationRoot $relative
        $statusCode = $line.Substring(0, 2)
        if ($statusCode.Contains("D")) {
            if (Test-Path -LiteralPath $target) {
                Remove-Item -LiteralPath $target -Force
                $removed.Add($relative) | Out-Null
            }

            continue
        }

        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            continue
        }

        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath $source -Destination $target -Force
        $copied.Add($relative) | Out-Null
    }

    return [ordered]@{
        copied = $copied.ToArray()
        removed = $removed.ToArray()
    }
}

function Get-UnityProcessesForProject {
    param([string]$ProjectPath)

    $normalized = [System.IO.Path]::GetFullPath($ProjectPath).TrimEnd('\').Replace('\', '/').ToLowerInvariant()
    Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $rawCommandLine = if ($null -eq $_.CommandLine) { "" } else { $_.CommandLine }
            $commandLine = $rawCommandLine.Replace('\', '/').ToLowerInvariant()
            $commandLine.Contains($normalized)
        }
}

function Wait-UnityBatch {
    param(
        [string]$ProjectPath,
        [string]$ResultsPath,
        [int]$TimeoutMinutes
    )

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    do {
        Start-Sleep -Seconds 5
        $exists = Test-Path -LiteralPath $ResultsPath
        $processes = @(Get-UnityProcessesForProject -ProjectPath $ProjectPath)
        Write-Host ("{0} Unity results={1} validationUnityProcesses={2}" -f (Get-Date -Format HH:mm:ss), $exists, $processes.Count)
        if ($exists -and $processes.Count -eq 0) {
            return
        }
    } while ((Get-Date) -lt $deadline)

    $remaining = @(Get-UnityProcessesForProject -ProjectPath $ProjectPath | Select-Object ProcessId, CommandLine)
    throw "Timed out waiting for Unity batch. Remaining processes: $($remaining | ConvertTo-Json -Compress)"
}

function Read-UnityResults {
    param([string]$ResultsPath)

    if (-not (Test-Path -LiteralPath $ResultsPath)) {
        throw "Unity test results were not written: $ResultsPath"
    }

    [xml]$xml = Get-Content -LiteralPath $ResultsPath
    $run = $xml.'test-run'
    return [ordered]@{
        result = [string]$run.result
        total = [int]$run.total
        passed = [int]$run.passed
        failed = [int]$run.failed
        skipped = [int]$run.skipped
        duration = [double]$run.duration
    }
}

function Get-UnityVersion {
    param([string]$UnityProjectRoot)

    $versionFile = Join-Path $UnityProjectRoot "ProjectSettings/ProjectVersion.txt"
    if (-not (Test-Path -LiteralPath $versionFile)) {
        return ""
    }

    $line = Get-Content -LiteralPath $versionFile | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($line)) {
        return ""
    }

    return $line.Substring($line.IndexOf(":") + 1).Trim()
}

$repositoryRoot = Resolve-RepositoryRoot
$unityExePath = Resolve-UnityExe -Requested $UnityExe
$head = Get-CurrentHead -RepositoryRoot $repositoryRoot
if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = "isolated-unity-" + (Get-Date -Format "yyyyMMdd-HHmmss")
}

if ([string]::IsNullOrWhiteSpace($WorktreePath)) {
    $WorktreePath = Join-Path $repositoryRoot ".tmp/unity-validation/worktree"
}

$WorktreePath = [System.IO.Path]::GetFullPath($WorktreePath)
$artifactRoot = Join-Path $repositoryRoot "artifacts/unity-validation/$RunId"
$logsRoot = Join-Path $artifactRoot "logs"
New-Item -ItemType Directory -Force -Path $logsRoot | Out-Null

Write-Host "WISM isolated Unity validation"
Write-Host "  Repository: $repositoryRoot"
Write-Host "  Head:       $head"
Write-Host "  Worktree:   $WorktreePath"
Write-Host "  Artifacts:  $artifactRoot"
Write-Host "  Unity:      $unityExePath"

Initialize-ValidationWorktree -RepositoryRoot $repositoryRoot -Path $WorktreePath -Head $head

$overlay = [ordered]@{ copied = @(); removed = @() }
if (-not $NoDirtyOverlay) {
    $overlay = Copy-DirtyOverlay -RepositoryRoot $repositoryRoot -ValidationRoot $WorktreePath -AllowedRoots $DirtyPath -ExcludedPatterns $ExcludeDirtyPath
}

$steps = New-Object System.Collections.Generic.List[object]
$wismClientRoot = Join-Path $WorktreePath "WismClient"
$unityProjectRoot = Join-Path $WorktreePath "WismUnity"

if (-not $SkipClientBuild) {
    $steps.Add((Invoke-Native -Name "dotnet-build" -FilePath "dotnet" -Arguments @("build", "WismClient.sln", "--configuration", "Release", "-v:minimal") -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "dotnet-build.log"))) | Out-Null
    if ($steps[-1].exitCode -ne 0) {
        throw "dotnet build failed. See $($steps[-1].logPath)"
    }
}

if (-not $SkipClientTests) {
    $steps.Add((Invoke-Native -Name "mod-kit-focused-tests" -FilePath "dotnet" -Arguments @("test", "Wism.Client.Test\Wism.Client.Test.csproj", "--configuration", "Release", "--no-build", "--filter", "ModKitValidatorTests|ModularProfileCatalogTests", "-v:minimal") -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "wismclient-modkit-tests.log"))) | Out-Null
    if ($steps[-1].exitCode -ne 0) {
        throw "Focused WismClient Mod Kit tests failed. See $($steps[-1].logPath)"
    }
}

$runBuildArgs = @("--configuration", "Release")
if (-not $SkipClientBuild) {
    $runBuildArgs += "--no-build"
}

$steps.Add((Invoke-Native -Name "mod-kit-validate" -FilePath "dotnet" -Arguments (@("run", "--project", "Wism.ModKit.Cli") + $runBuildArgs + @("--", "validate", "repo=$WorktreePath", "profile=$Profile", "packs=$Packs", "--json")) -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "modkit-validate.log"))) | Out-Null
if ($steps[-1].exitCode -ne 0) {
    throw "Mod Kit validation failed. See $($steps[-1].logPath)"
}

$steps.Add((Invoke-Native -Name "agent-playground" -FilePath "dotnet" -Arguments (@("run", "--project", "Wism.Agent.Playground") + $runBuildArgs + @("--", "world", "profile=$Profile", "packs=$Packs", "--quiet")) -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "agentplayground.log"))) | Out-Null
if ($steps[-1].exitCode -ne 0) {
    throw "AgentPlayground smoke failed. See $($steps[-1].logPath)"
}

$unityResults = Join-Path $artifactRoot "unity-playmode-results.xml"
$unityLog = Join-Path $artifactRoot "unity-playmode.log"
Remove-Item -LiteralPath $unityResults -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $unityLog -Force -ErrorAction SilentlyContinue

$existingValidationUnity = @(Get-UnityProcessesForProject -ProjectPath $unityProjectRoot)
if ($existingValidationUnity.Count -gt 0) {
    throw "Validation worktree is already open in Unity. Close that Unity instance or use a different -WorktreePath."
}

$unityStartedAt = (Get-Date).ToUniversalTime().ToString("O")
Write-Host "Starting Unity PlayMode tests in isolated worktree..."
& $unityExePath -batchmode -projectPath $unityProjectRoot -runTests -testPlatform PlayMode -testFilter $TestFilter -testResults $unityResults -logFile $unityLog
$unityLauncherExitCode = if ($null -eq $global:LASTEXITCODE) { 0 } else { $global:LASTEXITCODE }
Wait-UnityBatch -ProjectPath $unityProjectRoot -ResultsPath $unityResults -TimeoutMinutes $TimeoutMinutes
$unityEndedAt = (Get-Date).ToUniversalTime().ToString("O")
$unityTest = Read-UnityResults -ResultsPath $unityResults
if ($unityTest.failed -ne 0 -or -not [string]::Equals($unityTest.result, "Passed", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unity PlayMode tests failed: $($unityTest | ConvertTo-Json -Compress). See $unityResults and $unityLog"
}

$unityManifestPath = Join-Path $artifactRoot "unity-validation-manifest.json"
$unityManifest = [ordered]@{
    schemaVersion = 1
    status = "Passed"
    startedAtUtc = $unityStartedAt
    endedAtUtc = $unityEndedAt
    unityVersion = Get-UnityVersion -UnityProjectRoot $unityProjectRoot
    projectPath = $unityProjectRoot
    profile = $Profile
    world = $World
    packs = @($Packs.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    testFilter = $TestFilter
    testResults = $unityResults
    launcherExitCode = $unityLauncherExitCode
    dirtyScenes = @()
    console = [ordered]@{
        errors = 0
        warnings = 0
    }
    proofNotes = "Isolated Unity batchmode PlayMode validation completed against a separate git worktree."
}
$unityManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $unityManifestPath

$proofSummary = ""
if (-not $SkipProof) {
    $proofRoot = Join-Path $artifactRoot "proof"
    $steps.Add((Invoke-Native -Name "mod-kit-proof" -FilePath "dotnet" -Arguments (@("run", "--project", "Wism.ModKit.Cli") + $runBuildArgs + @("--", "proof", "repo=$WorktreePath", "profile=$Profile", "packs=$Packs", "runId=summary", "out=$proofRoot", "unityManifest=$unityManifestPath", "unityStatusManifest=$unityManifestPath", "unityTestResults=$unityResults")) -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "modkit-proof.log"))) | Out-Null
    if ($steps[-1].exitCode -ne 0) {
        throw "Mod Kit proof failed. See $($steps[-1].logPath)"
    }

    $proofSummary = Join-Path $proofRoot "summary/proof-summary.json"
}

$summaryPath = Join-Path $artifactRoot "isolated-validation-summary.json"
$summary = [ordered]@{
    schemaVersion = 1
    status = "Passed"
    runId = $RunId
    repositoryRoot = $repositoryRoot
    validationWorktree = $WorktreePath
    head = $head
    dirtyOverlay = $overlay
    unity = [ordered]@{
        projectPath = $unityProjectRoot
        resultsPath = $unityResults
        logPath = $unityLog
        manifestPath = $unityManifestPath
        result = $unityTest
    }
    proofSummary = $proofSummary
    steps = $steps.ToArray()
}
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summaryPath

Write-Host "Isolated Unity validation: Passed"
Write-Host "  Summary: $summaryPath"
Write-Host "  Unity results: $unityResults"
if (-not [string]::IsNullOrWhiteSpace($proofSummary)) {
    Write-Host "  Proof: $proofSummary"
}
