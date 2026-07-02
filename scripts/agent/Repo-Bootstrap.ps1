#Requires -Version 5.1
<#
.SYNOPSIS
  Read-only bootstrap check for agents entering the EventHub repo.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Test-CommandAvailable {
    param([string]$Name)
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    return $null -ne $cmd
}

function Get-ExecutableForCommand {
    param([string]$Name)
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        return $null
    }

    $source = if ($cmd.Source) { [string]$cmd.Source } elseif ($cmd.Path) { [string]$cmd.Path } else { [string]$cmd.Definition }
    if ($source -and $source.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase)) {
        $cmdSibling = [System.IO.Path]::ChangeExtension($source, '.cmd')
        if (Test-Path -LiteralPath $cmdSibling) {
            return $cmdSibling
        }
    }

    if ($source) {
        return $source
    }
    return $Name
}

function Get-CommandVersion {
    param(
        [string]$Name,
        [string[]]$ArgumentList = @('--version')
    )
    if (-not (Test-CommandAvailable -Name $Name)) {
        return $null
    }
    try {
        $executable = Get-ExecutableForCommand -Name $Name
        if (-not $executable) {
            return $null
        }

        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $executable
        $psi.Arguments = ($ArgumentList -join ' ')
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true

        $proc = [System.Diagnostics.Process]::Start($psi)
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()

        $lines = (($stdout + "`n" + $stderr) -split "`r?`n") | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        }
        $first = $lines | Select-Object -First 1
        if ($null -eq $first) {
            return $null
        }
        return [string]$first
    }
    catch {
        return $null
    }
}

$checks = @(
    @{ name = 'dotnet'; ok = (Test-CommandAvailable -Name 'dotnet'); version = (Get-CommandVersion -Name 'dotnet' -ArgumentList @('--version')) },
    @{ name = 'node'; ok = (Test-CommandAvailable -Name 'node'); version = (Get-CommandVersion -Name 'node' -ArgumentList @('--version')) },
    @{ name = 'yarn'; ok = (Test-CommandAvailable -Name 'yarn'); version = (Get-CommandVersion -Name 'yarn' -ArgumentList @('--version')) },
    @{ name = 'git'; ok = (Test-CommandAvailable -Name 'git'); version = (Get-CommandVersion -Name 'git' -ArgumentList @('--version')) },
    @{ name = 'aspire'; ok = (Test-CommandAvailable -Name 'aspire'); version = (Get-CommandVersion -Name 'aspire' -ArgumentList @('--version')) }
)

$requiredPaths = @(
    'AGENTS.md',
    'EventHub.slnx',
    'src/AppHost',
    'src/ServiceDefaults',
    'web/package.json',
    'scripts/affected-tests.mjs',
    '.codex/harness.toml',
    '.codex/policies/harness-policy.json'
)

$pathChecks = foreach ($rel in $requiredPaths) {
    @{
        path = $rel
        ok = Test-Path -LiteralPath (Join-Path $repoRoot ($rel -replace '/', '\'))
    }
}

$missingTools = @($checks | Where-Object { -not $_.ok } | ForEach-Object { $_.name })
$missingPaths = @($pathChecks | Where-Object { -not $_.ok } | ForEach-Object { $_.path })

$result = @{
    repoRoot = $repoRoot
    status = if ($missingTools.Count -eq 0 -and $missingPaths.Count -eq 0) { 'ok' } else { 'attention' }
    tools = $checks
    requiredPaths = $pathChecks
    next = @(
        'Use Aspire AppHost for local topology.',
        'Use scripts/agent/Verify-ChangedCode.ps1 before handoff.',
        'Do not edit .env, web/src/generated, or OpenAPI build output.'
    )
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host ''
Write-Host 'EventHub repo bootstrap' -ForegroundColor Cyan
Write-Host "  status: $($result.status)"
Write-Host "  root:   $repoRoot"
Write-Host ''
Write-Host 'Tools'
foreach ($check in $checks) {
    $mark = if ($check.ok) { 'ok' } else { 'missing' }
    $version = if ($check.version) { " ($($check.version))" } else { '' }
    Write-Host "  [$mark] $($check.name)$version"
}
Write-Host ''
Write-Host 'Required paths'
foreach ($check in $pathChecks) {
    $mark = if ($check.ok) { 'ok' } else { 'missing' }
    Write-Host "  [$mark] $($check.path)"
}
Write-Host ''
Write-Host 'Next'
foreach ($item in $result.next) {
    Write-Host "  - $item"
}

if ($result.status -ne 'ok') {
    exit 1
}
exit 0
