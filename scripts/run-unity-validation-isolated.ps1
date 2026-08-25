[CmdletBinding()]
param(
    [string]$UnityCli = "unity",
    [string]$WorktreePath = "",
    [string]$RunId = "",
    [string]$TestFilter = "",
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
    [switch]$EnableCoverage,
    [string]$CoverageOptions = "generateHtmlReport;generateAdditionalMetrics;assemblyFilters:+UnityGame",
    [int]$TimeoutMinutes = 10,
    [ValidateRange(0, 2)]
    [int]$TestRetries = 1
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

function Resolve-UnityCli {
    param([string]$Requested)

    $command = Get-Command $Requested -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    if ($Requested -eq "unity") {
        $windowsAppsCli = Join-Path $env:LOCALAPPDATA "Microsoft\WindowsApps\unity.exe"
        if (Test-Path -LiteralPath $windowsAppsCli) {
            return $windowsAppsCli
        }
    }

    throw "Unity CLI was not found. Install it with: winget install Unity.CLI"
}

function Invoke-Native {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [string]$LogPath,
        [switch]$CaptureOutput
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $LogPath) | Out-Null
    "[$(Get-Date -Format o)] $Name" | Set-Content -LiteralPath $LogPath
    "WorkingDirectory: $WorkingDirectory" | Add-Content -LiteralPath $LogPath
    "Command: $FilePath $($Arguments -join ' ')" | Add-Content -LiteralPath $LogPath
    "" | Add-Content -LiteralPath $LogPath

    $code = 1
    $captured = @()
    $previousErrorActionPreference = $ErrorActionPreference
    Push-Location $WorkingDirectory
    try {
        # Windows PowerShell 5 wraps native stderr as ErrorRecord objects. Keep
        # those records in the log and let the process exit code classify them.
        $ErrorActionPreference = "Continue"
        if ($CaptureOutput) {
            $captured = @(& $FilePath @Arguments 2>&1 | ForEach-Object { $_.ToString() })
            $captured | Add-Content -LiteralPath $LogPath
        }
        else {
            & $FilePath @Arguments *>> $LogPath
        }
        $code = if ($null -eq $global:LASTEXITCODE) { 0 } else { $global:LASTEXITCODE }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        Pop-Location
    }

    return [ordered]@{
        name = $Name
        exitCode = $code
        logPath = $LogPath
        standardOutput = $captured
    }
}

function Clear-UnityPackageCacheTemps {
    param([string]$UnityProjectRoot)

    $packageCache = Join-Path $UnityProjectRoot "Library\PackageCache"
    if (-not (Test-Path -LiteralPath $packageCache)) {
        return
    }

    Get-ChildItem -LiteralPath $packageCache -Directory -Filter ".tmp-*" -ErrorAction SilentlyContinue |
        ForEach-Object {
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
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

function Convert-RenameSourcePath {
    param([string]$StatusLine)

    if ($StatusLine.Length -lt 4) {
        return ""
    }

    $path = $StatusLine.Substring(3)
    if (-not $path.Contains(" -> ")) {
        return ""
    }

    return ($path -split " -> ")[0].Trim('"')
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

        $statusCode = $line.Substring(0, 2)
        if ($statusCode.Contains("R")) {
            $renameSource = Convert-RenameSourcePath -StatusLine $line
            if (-not [string]::IsNullOrWhiteSpace($renameSource) -and
                (Test-AllowedDirtyPath -RelativePath $renameSource -AllowedRoots $AllowedRoots) -and
                -not (Test-ExcludedDirtyPath -RelativePath $renameSource -ExcludedPatterns $ExcludedPatterns)) {
                $renameSourceTarget = Join-Path $ValidationRoot $renameSource
                if (Test-Path -LiteralPath $renameSourceTarget) {
                    Remove-Item -LiteralPath $renameSourceTarget -Force
                    $removed.Add($renameSource) | Out-Null
                }
            }
        }

        $source = Join-Path $RepositoryRoot $relative
        $target = Join-Path $ValidationRoot $relative
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
$unityCliPath = Resolve-UnityCli -Requested $UnityCli
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
Write-Host "  Unity CLI:  $unityCliPath"

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

$steps.Add((Invoke-Native -Name "mod-kit-validate" -FilePath "dotnet" -Arguments (@("run", "--project", "Wism.ModKit.Cli") + $runBuildArgs + @("--", "validate", "repo=$WorktreePath", "profile=$Profile", "packs=$Packs", "--json")) -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "modkit-validate.log") -CaptureOutput)) | Out-Null
if ($steps[-1].exitCode -ne 0) {
    throw "Mod Kit validation failed. See $($steps[-1].logPath)"
}
$modKitValidation = ($steps[-1].standardOutput -join [Environment]::NewLine) | ConvertFrom-Json
if ($null -eq $modKitValidation -or -not $modKitValidation.IsValid) {
    throw "Mod Kit validation did not return a valid structured result. See $($steps[-1].logPath)"
}

$steps.Add((Invoke-Native -Name "agent-playground" -FilePath "dotnet" -Arguments (@("run", "--project", "Wism.Agent.Playground") + $runBuildArgs + @("--", "world", "profile=$Profile", "packs=$Packs", "--quiet")) -WorkingDirectory $wismClientRoot -LogPath (Join-Path $logsRoot "agentplayground.log"))) | Out-Null
if ($steps[-1].exitCode -ne 0) {
    throw "AgentPlayground smoke failed. See $($steps[-1].logPath)"
}

$unityResults = Join-Path $artifactRoot "unity-playmode-results.xml"
$unityJunitResults = Join-Path $artifactRoot "unity-playmode-results.junit.xml"
$unityLog = Join-Path $logsRoot "unity-test-cli.log"
Remove-Item -LiteralPath $unityResults -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $unityJunitResults -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $unityLog -Force -ErrorAction SilentlyContinue

$existingValidationUnity = @(Get-UnityProcessesForProject -ProjectPath $unityProjectRoot)
if ($existingValidationUnity.Count -gt 0) {
    throw "Validation worktree is already open in Unity. Close that Unity instance or use a different -WorktreePath."
}

Clear-UnityPackageCacheTemps -UnityProjectRoot $unityProjectRoot

$steps.Add((Invoke-Native -Name "unity-cli-doctor" -FilePath $unityCliPath -Arguments @("doctor", "--ci", "--format", "json", "--non-interactive") -WorkingDirectory $unityProjectRoot -LogPath (Join-Path $logsRoot "unity-cli-doctor.log"))) | Out-Null
if ($steps[-1].exitCode -ne 0) {
    throw "Unity CLI preflight failed. See $($steps[-1].logPath)"
}

$unityStartedAt = (Get-Date).ToUniversalTime().ToString("O")
Write-Host "Starting Unity CLI PlayMode tests in isolated worktree..."
$unityArguments = @(
    "test", $unityProjectRoot,
    "--mode", "PlayMode",
    "--output", $unityResults,
    "--report-format", "nunit,junit",
    "--junit-output", $unityJunitResults,
    "--retries", $TestRetries,
    "--timeout", ($TimeoutMinutes * 60),
    "--format", "ndjson",
    "--non-interactive"
)
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $unityArguments += @("--filter", $TestFilter)
}
if ($EnableCoverage) {
    $coverageRoot = Join-Path $artifactRoot "coverage"
    $unityArguments += @(
        "--coverage",
        "--coverage-output", $coverageRoot,
        "--coverage-options", $CoverageOptions
    )
}
$steps.Add((Invoke-Native -Name "unity-cli-playmode-tests" -FilePath $unityCliPath -Arguments $unityArguments -WorkingDirectory $unityProjectRoot -LogPath $unityLog)) | Out-Null
$unityLauncherExitCode = $steps[-1].exitCode
if ($unityLauncherExitCode -eq 8) {
    throw "Unity PlayMode tests completed with failures. See $unityResults and $unityLog"
}
if ($unityLauncherExitCode -ne 0) {
    throw "Unity PlayMode tests did not produce a valid verdict (exit $unityLauncherExitCode). See $unityLog"
}
$unityEndedAt = (Get-Date).ToUniversalTime().ToString("O")
$unityTest = Read-UnityResults -ResultsPath $unityResults
if ($unityTest.failed -ne 0 -or -not [string]::Equals($unityTest.result, "Passed", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unity PlayMode tests failed: $($unityTest | ConvertTo-Json -Compress). See $unityResults and $unityLog"
}

$retrySummaryCandidate = Join-Path (Split-Path -Parent $unityResults) (([System.IO.Path]::GetFileNameWithoutExtension($unityResults)) + ".retries.json")
$retrySummary = if (Test-Path -LiteralPath $retrySummaryCandidate) { $retrySummaryCandidate } else { $null }

$unityManifestPath = Join-Path $artifactRoot "unity-validation-manifest.json"
$unityManifest = [ordered]@{
    schemaVersion = 1
    status = "Passed"
    startedAtUtc = $unityStartedAt
    endedAtUtc = $unityEndedAt
    unityVersion = Get-UnityVersion -UnityProjectRoot $unityProjectRoot
    projectPath = $unityProjectRoot
    profile = $Profile
    world = $modKitValidation.LaunchWorld
    requestedWorld = $World
    packs = @($Packs.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    selection = [ordered]@{
        profileId = $modKitValidation.ProfileId
        worldName = $modKitValidation.LaunchWorld
        compatibilityStatus = $modKitValidation.CompatibilityStatus
        isGreen = [bool]$modKitValidation.IsGreen
        isLoadable = [bool]$modKitValidation.IsLoadable
        contentFingerprint = $modKitValidation.ContentFingerprint
    }
    testFilter = $TestFilter
    testResults = $unityResults
    junitTestResults = $unityJunitResults
    retrySummary = $retrySummary
    launcherExitCode = $unityLauncherExitCode
    dirtyScenes = @()
    console = [ordered]@{
        errors = 0
        warnings = 0
    }
    proofNotes = "Isolated Unity CLI PlayMode validation completed against a separate git worktree. Exit 8 is a terminal test failure; infrastructure failures are reported separately."
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
        junitResultsPath = $unityJunitResults
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
