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
. "$repoRoot\.codex\hooks\lib\verify-runner.ps1"

function Convert-StepToCommand {
    param([object]$Step)
    switch ($Step.kind) {
        'eslint' {
            $webRel = [string]$Step.file
            if ($webRel.StartsWith('web/')) { $webRel = $webRel.Substring(4) }
            return "yarn --cwd web eslint $webRel --max-warnings 0"
        }
        'dotnet-format' {
            return "dotnet format EventHub.slnx --verify-no-changes --include $($Step.file)"
        }
        'dotnet-test' {
            $cmd = "dotnet test $($Step.project)"
            if ($Step.filter) { $cmd += " --filter $($Step.filter)" }
            return $cmd
        }
        'dotnet-build' {
            return "dotnet build $($Step.project) -v q"
        }
        'shell-test' {
            return [string]$Step.command
        }
        default {
            return "unknown step: $($Step.kind)"
        }
    }
}

function Get-InputFiles {
    if ($Path -and $Path.Count -gt 0) {
        return @($Path)
    }
    return @(Get-GitChangedFiles -ProjectRoot $repoRoot)
}

$files = @(Get-InputFiles)
$stepKeys = @{}
$steps = New-Object System.Collections.Generic.List[object]
$mappedFiles = New-Object System.Collections.Generic.List[object]
$needsTypeCheck = $false

foreach ($file in $files) {
    $rel = $file -replace '\\', '/'
    if ($rel -match '^web/.*\.(tsx?|jsx?)$') {
        $needsTypeCheck = $true
    }

    $abs = if ([System.IO.Path]::IsPathRooted($file)) {
        $file
    }
    else {
        Join-Path $repoRoot ($file -replace '/', '\')
    }

    $plan = Get-AffectedPlan -ProjectRoot $repoRoot -FilePath $abs
    $fileSteps = @()
    if ($plan -and $plan.steps) {
        $fileSteps = @($plan.steps | ForEach-Object { Convert-StepToCommand $_ })
    }

    $mappedFiles.Add(@{
        file = $rel
        skip = ($null -eq $plan -or $plan.skip -or -not $plan.steps)
        steps = $fileSteps
    })

    if ($null -eq $plan -or $plan.skip -or -not $plan.steps) {
        continue
    }

    foreach ($step in @($plan.steps)) {
        $key = Get-StepKey -Step $step
        if (-not $stepKeys.ContainsKey($key)) {
            $stepKeys[$key] = $true
            $steps.Add($step)
        }
    }
}

$commands = @()
if ($needsTypeCheck) {
    $commands += 'yarn --cwd web exec tsc -b --noEmit'
}
foreach ($step in $steps) {
    $commands += Convert-StepToCommand $step
}

$errors = New-Object System.Collections.Generic.List[string]
if (-not $PlanOnly) {
    if ($needsTypeCheck) {
        foreach ($err in (Test-WebTypeCheck -ProjectRoot $repoRoot)) {
            $errors.Add($err)
        }
    }
    foreach ($err in (Invoke-VerificationSteps -ProjectRoot $repoRoot -Steps $steps.ToArray())) {
        $errors.Add($err)
    }
}

$result = @{
    repoRoot = $repoRoot
    files = $mappedFiles
    commands = $commands
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
foreach ($cmd in $commands) {
    Write-Host "  - $cmd"
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
