#Requires -Version 5.1
<#
.SYNOPSIS
  Validate harness policy and verification routing guardrails.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. "$repoRoot\.codex\hooks\lib\hook-io.ps1"
. "$repoRoot\.codex\hooks\lib\guard-rules.ps1"
. "$repoRoot\.codex\hooks\lib\verify-runner.ps1"

$errors = New-Object System.Collections.Generic.List[string]

function Add-Error {
    param([string]$Message)
    $script:errors.Add($Message) | Out-Null
}

function Test-VerifyExpectation {
    param(
        [string]$RelativePath,
        [bool]$Expected
    )

    $fullPath = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    $actual = Test-ShouldVerifyFile -FilePath $fullPath -ProjectRoot $repoRoot
    if ($actual -ne $Expected) {
        Add-Error "Verify routing for $RelativePath expected $Expected, got $actual"
    }
}

foreach ($path in @(
    'AGENTS.md',
    '.agents/skills/spec/SKILL.md',
    '.codex/policies/harness-policy.json',
    '.graph/index.json',
    'docs/_memory/source/harness-architecture.md',
    'docs/_memory/specs/README.md',
    'scripts/agent/Test-DocsMemory.ps1',
    'evals/cases/harness-docs-memory-lifecycle.json'
)) {
    Test-VerifyExpectation -RelativePath $path -Expected $true
}

foreach ($path in @(
    '.github/workflows/ci.yml',
    'web/src/generated/api.ts',
    'src/Infrastructure/Migrations/20260601000000_Test.cs'
)) {
    Test-VerifyExpectation -RelativePath $path -Expected $false
}

foreach ($path in @(
    'web/src/generated/api.ts',
    'contracts/openapi/.build/api.v1.yaml',
    '.env.local',
    '.mcp.json'
)) {
    if (-not (Test-BlockedEditPath -Path $path)) {
        Add-Error "Protected edit path was not blocked: $path"
    }
}

foreach ($command in @(
    'npm install',
    'git reset --hard',
    'git push --force',
    'Remove-Item -Recurse -Force temp'
)) {
    if (-not (Test-DangerousShellCommand -Command $command)) {
        Add-Error "Dangerous shell command was not blocked: $command"
    }
}

$quoted = ConvertTo-ProcessArgumentString -ArgumentList @('one', 'two words', 'quote"inside')
if ($quoted -notmatch '"two words"' -or $quoted -notmatch '"quote\\"inside"') {
    Add-Error "Process argument quoting did not preserve spaces and quotes: $quoted"
}

$result = @{
    status = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    errors = @($errors)
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
}
else {
    Write-Host ''
    Write-Host 'EventHub harness policy validation' -ForegroundColor Cyan
    Write-Host "  status: $($result.status)"
    if ($errors.Count -gt 0) {
        Write-Host ''
        Write-Host 'Errors' -ForegroundColor Red
        foreach ($err in $errors) {
            Write-Host "  - $err" -ForegroundColor Red
        }
    }
}

if ($errors.Count -gt 0) {
    exit 1
}
exit 0
