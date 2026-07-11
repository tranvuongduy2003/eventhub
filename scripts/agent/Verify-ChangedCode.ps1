#Requires -Version 5.1
<#
.SYNOPSIS
  Agent-friendly verification runner for changed EventHub files.
#>

param(
    [string[]]$Path,
    [switch]$PlanOnly,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location -LiteralPath $repoRoot

function ConvertTo-RelativePath {
    param([string]$Value)

    $fullPath = if ([System.IO.Path]::IsPathRooted($Value)) {
        [System.IO.Path]::GetFullPath($Value)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot ($Value -replace '/', '\')))
    }

    $root = [System.IO.Path]::GetFullPath($repoRoot)
    if (-not $root.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $root += [System.IO.Path]::DirectorySeparatorChar
    }

    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($root.Length).Replace('\', '/')
    }

    return $Value.Replace('\', '/')
}

function Get-GitChangedFiles {
    $files = @()
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $files += (& git diff --name-only HEAD 2>$null)
        $files += (& git ls-files --others --exclude-standard 2>$null)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    return @($files | Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
}

function Add-Command {
    param(
        [System.Collections.Generic.List[string]]$Commands,
        [hashtable]$Seen,
        [string]$Command
    )

    if (-not $Seen.ContainsKey($Command)) {
        $Seen[$Command] = $true
        $Commands.Add($Command) | Out-Null
    }
}

$files = if ($Path -and $Path.Count -gt 0) {
    @($Path | ForEach-Object { ConvertTo-RelativePath $_ })
}
else {
    @(Get-GitChangedFiles)
}

$commands = [System.Collections.Generic.List[string]]::new()
$seen = @{}
$mappedFiles = [System.Collections.Generic.List[object]]::new()

$backendChanged = $false
$webChanged = $false
$e2eChanged = $false
$contractChanged = $false
$docsOrHarnessChanged = $false

foreach ($file in $files) {
    $steps = [System.Collections.Generic.List[string]]::new()

    if ($file -match '^src/.*\.cs$' -or $file -match '^tests/.*\.cs$') {
        $backendChanged = $true
        $steps.Add('dotnet build EventHub.slnx -c Release') | Out-Null
    }
    elseif ($file -match '^web/.*\.(ts|tsx|js|jsx|css)$' -or $file -eq 'web/package.json' -or $file -eq 'web/yarn.lock') {
        $webChanged = $true
        $steps.Add('yarn --cwd web build') | Out-Null
    }
    elseif ($file -match '^e2e/.*\.(ts|tsx|js|jsx)$' -or $file -eq 'e2e/package.json' -or $file -eq 'e2e/yarn.lock') {
        $e2eChanged = $true
        $steps.Add('yarn --cwd e2e test') | Out-Null
    }

    if ($file -match '^contracts/openapi/' -or $file -match '^src/Contracts/' -or $file -match '^src/Api/Endpoints/' -or $file -match '^web/src/lib/api/' -or $file -match '^web/src/generated/') {
        $contractChanged = $true
        $steps.Add('yarn --cwd web api:verify') | Out-Null
    }

    if ($file -match '(^|/)AGENTS\.md$' -or $file -match '^(docs/|\.codex/config\.toml$|\.codex/agents/|\.codex/hooks/|\.codex/hooks\.json$|\.agents/skills/|scripts/agent/)') {
        $docsOrHarnessChanged = $true
        $steps.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1') | Out-Null
        $steps.Add('powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1') | Out-Null
    }

    foreach ($step in $steps) {
        Add-Command -Commands $commands -Seen $seen -Command $step
    }

    $mappedFiles.Add([pscustomobject]@{
        file = $file
        skip = ($steps.Count -eq 0)
        steps = @($steps)
    }) | Out-Null
}

if ($backendChanged) {
    Add-Command -Commands $commands -Seen $seen -Command 'dotnet build EventHub.slnx -c Release'
}
if ($webChanged -or $contractChanged) {
    Add-Command -Commands $commands -Seen $seen -Command 'yarn --cwd web build'
}
if ($e2eChanged) {
    Add-Command -Commands $commands -Seen $seen -Command 'yarn --cwd e2e test'
}
if ($contractChanged) {
    Add-Command -Commands $commands -Seen $seen -Command 'yarn --cwd web api:verify'
}
if ($docsOrHarnessChanged) {
    Add-Command -Commands $commands -Seen $seen -Command 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1'
    Add-Command -Commands $commands -Seen $seen -Command 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1'
}

$errors = [System.Collections.Generic.List[string]]::new()
if (-not $PlanOnly) {
    foreach ($command in $commands) {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $output = & powershell -NoProfile -ExecutionPolicy Bypass -Command $command 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        if ($exitCode -ne 0) {
            $tail = $output | Select-Object -Last 40 | Out-String
            $errors.Add("$command failed with exit code ${exitCode}:`n$tail") | Out-Null
        }
    }
}

$result = @{
    repoRoot = $repoRoot
    files = @($mappedFiles)
    commands = @($commands)
    status = if ($PlanOnly) { 'planned' } elseif ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    errors = @($errors)
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

$stateDir = Join-Path $repoRoot '.codex\state'
if (-not (Test-Path -LiteralPath $stateDir)) {
    New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
}
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $stateDir 'verify-changed-code-latest.json') -Encoding utf8

if ($Json) {
    $result | ConvertTo-Json -Depth 10
    if ($errors.Count -gt 0) { exit 1 }
    exit 0
}

Write-Host ''
Write-Host 'EventHub changed-code verification' -ForegroundColor Cyan
Write-Host "  status: $($result.status)"
Write-Host ''
Write-Host 'Commands'
if ($commands.Count -eq 0) {
    Write-Host '  (none)'
}
foreach ($command in $commands) {
    Write-Host "  - $command"
}
if ($errors.Count -gt 0) {
    Write-Host ''
    Write-Host 'Errors' -ForegroundColor Red
    foreach ($err in $errors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}
exit 0
