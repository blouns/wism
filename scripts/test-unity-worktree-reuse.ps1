[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'run-unity-validation-isolated.ps1'
$tokens = $null
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw "Validation script must parse." }
foreach ($name in @('Get-CurrentHead', 'Test-RegisteredWorktree', 'Initialize-ValidationWorktree')) {
    $function = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
    if (-not $function) { throw "Missing $name" }
    . ([scriptblock]::Create($function.Extent.Text))
}
function Get-UnityProcessesForProject { param($ProjectPath) if ($script:LiveEditor) { return @(1) } }
function Expect-Refusal([scriptblock]$Action, [string]$Message) {
    try { & $Action } catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        $script:Passed++
        return
    }
    throw "Expected refusal: $Message"
}
function Invoke-CheckedGit([string[]]$Arguments) {
    $result = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
    return $result
}
$root = Join-Path ([IO.Path]::GetTempPath()) ('wism-reuse-test-' + [guid]::NewGuid().ToString('N'))
$repo = Join-Path $root 'repo'
$worktree = Join-Path $root 'candidate'
$Passed = 0
try {
    New-Item -ItemType Directory -Path $repo -Force | Out-Null
    Invoke-CheckedGit @('init', $repo) | Out-Null
    [IO.File]::WriteAllText((Join-Path $repo '.gitignore'), "WismUnity/Library/`nWismUnity/Temp/`n")
    Invoke-CheckedGit @('-C', $repo, 'add', '.gitignore') | Out-Null
    Invoke-CheckedGit @('-C', $repo, '-c', 'user.name=Validation Test', '-c', 'user.email=validation@example.invalid', 'commit', '-m', 'fixture') | Out-Null
    $head = Get-CurrentHead $repo
    Invoke-CheckedGit @('-C', $repo, 'worktree', 'add', '--detach', $worktree, $head) | Out-Null
    $library = Join-Path $worktree 'WismUnity/Library'
    New-Item -ItemType Directory -Path $library -Force | Out-Null
    $sentinel = Join-Path $library 'retained.cache'
    [IO.File]::WriteAllText($sentinel, 'must survive')
    $ReuseExistingWorktree = $true
    $NoDirtyOverlay = $true
    $LiveEditor = $false
    Initialize-ValidationWorktree $repo $worktree $head
    if ([IO.File]::ReadAllText($sentinel) -ne 'must survive') { throw 'Library was changed.' }
    $Passed++
    $ReuseExistingWorktree = $false
    Expect-Refusal { Initialize-ValidationWorktree $repo $worktree $head } 'require -ReuseExistingWorktree'
    $ReuseExistingWorktree = $true
    $NoDirtyOverlay = $false
    Expect-Refusal { Initialize-ValidationWorktree $repo $worktree $head } 'NoDirtyOverlay'
    $NoDirtyOverlay = $true
    Expect-Refusal { Initialize-ValidationWorktree $repo $worktree ('a' * 40) } 'HEAD must match'
    [IO.File]::WriteAllText((Join-Path $worktree 'human.txt'), 'preserve this')
    Expect-Refusal { Initialize-ValidationWorktree $repo $worktree $head } 'must be clean'
    Remove-Item -LiteralPath (Join-Path $worktree 'human.txt')
    $LiveEditor = $true
    Expect-Refusal { Initialize-ValidationWorktree $repo $worktree $head } 'live Editor'
    $LiveEditor = $false
    $temp = Join-Path $worktree 'WismUnity/Temp'
    New-Item -ItemType Directory -Path $temp -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $temp 'UnityLockfile'), '')
    Expect-Refusal { Initialize-ValidationWorktree $repo $worktree $head } 'locked'
    Expect-Refusal { Initialize-ValidationWorktree $repo (Join-Path $root 'absent') $head } 'does not exist'
    if ([IO.File]::ReadAllText($sentinel) -ne 'must survive') { throw 'Refusals damaged Library.' }
    "Passed $Passed safe worktree reuse cases. No Unity launched."
} finally {
    $resolved = [IO.Path]::GetFullPath($root)
    $prefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'wism-reuse-test-*') { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
