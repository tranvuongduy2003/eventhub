#Requires -Version 5.1
<#
.SYNOPSIS
  Run agent eval cases - objective pass/fail for harness, graph, and agent layers.

.DESCRIPTION
  Each case in evals/cases/*.json defines input + assert criteria.
  Deterministic cases (harness, graph) run in CI; agent cases with mode=manual are skipped unless -IncludeAgent.

.EXAMPLE
  .\evals\run.ps1
  .\evals\run.ps1 -Layer harness
  .\evals\run.ps1 -CaseId harness-pre-tool-block-generated -Json
#>

param(
    [string]$Layer,
    [string]$CaseId,
    [switch]$IncludeAgent,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$casesDir = Join-Path $PSScriptRoot 'cases'
$resultsDir = Join-Path $PSScriptRoot 'results'
$powerShellStateDir = Join-Path $repoRoot '.codex\state\powershell'
$powerShellLocalAppData = Join-Path $powerShellStateDir 'LocalAppData'
$powerShellRoamingAppData = Join-Path $powerShellStateDir 'AppData'
$powerShellModuleAnalysisCache = Join-Path $powerShellLocalAppData 'Microsoft\Windows\PowerShell\ModuleAnalysisCache'

function Initialize-PowerShellCacheEnvironment {
    foreach ($dir in @($powerShellLocalAppData, $powerShellRoamingAppData)) {
        if (-not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
        }
    }

    $env:LOCALAPPDATA = $powerShellLocalAppData
    $env:APPDATA = $powerShellRoamingAppData
    $env:PSModuleAnalysisCachePath = $powerShellModuleAnalysisCache
}

function Remove-RepoRootPowerShellCacheArtifact {
    $cachePath = Join-Path $repoRoot 'Microsoft\Windows\PowerShell\ModuleAnalysisCache'
    if (Test-Path -LiteralPath $cachePath) {
        Remove-Item -LiteralPath $cachePath -Force -ErrorAction SilentlyContinue
    }

    foreach ($dir in @(
        (Join-Path $repoRoot 'Microsoft\Windows\PowerShell'),
        (Join-Path $repoRoot 'Microsoft\Windows'),
        (Join-Path $repoRoot 'Microsoft')
    )) {
        if ((Test-Path -LiteralPath $dir) -and -not (Get-ChildItem -LiteralPath $dir -Force -ErrorAction SilentlyContinue | Select-Object -First 1)) {
            Remove-Item -LiteralPath $dir -Force -ErrorAction SilentlyContinue
        }
    }
}

function Set-ProcessPowerShellCacheEnvironment {
    param([System.Diagnostics.ProcessStartInfo]$ProcessStartInfo)

    $ProcessStartInfo.EnvironmentVariables['LOCALAPPDATA'] = $powerShellLocalAppData
    $ProcessStartInfo.EnvironmentVariables['APPDATA'] = $powerShellRoamingAppData
    $ProcessStartInfo.EnvironmentVariables['PSModuleAnalysisCachePath'] = $powerShellModuleAnalysisCache
}

Initialize-PowerShellCacheEnvironment
Remove-RepoRootPowerShellCacheArtifact

function Get-HookPowerShellExe {
    # Reuse the interpreter running this script (pwsh on Linux CI, pwsh or Windows PowerShell locally).
    try {
        return (Get-Process -Id $PID).Path
    }
    catch {
        $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($pwsh) { return $pwsh.Source }
        return (Get-Command powershell -ErrorAction Stop).Source
    }
}

$script:HookPowerShellExe = Get-HookPowerShellExe

function ConvertTo-ProcessArgumentString {
    param([string[]]$ArgumentList)

    $quoted = foreach ($arg in @($ArgumentList)) {
        if ($null -eq $arg) {
            continue
        }

        $value = [string]$arg
        if ($value -notmatch '[\s"]') {
            $value
            continue
        }

        $escaped = $value -replace '(\\*)"', '$1$1\"'
        $escaped = $escaped -replace '(\\+)$', '$1$1'
        '"' + $escaped + '"'
    }

    return ($quoted -join ' ')
}

function powershell {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [object]$InputObject,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$ArgumentList
    )

    begin {
        $pipelineInput = New-Object System.Collections.Generic.List[string]
    }
    process {
        if ($null -ne $InputObject) {
            $pipelineInput.Add([string]$InputObject) | Out-Null
        }
    }
    end {
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $script:HookPowerShellExe
        $psi.Arguments = ConvertTo-ProcessArgumentString -ArgumentList $ArgumentList
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        $psi.WorkingDirectory = (Get-Location).Path
        Set-ProcessPowerShellCacheEnvironment -ProcessStartInfo $psi

        $proc = [System.Diagnostics.Process]::Start($psi)
        if ($pipelineInput.Count -gt 0) {
            $proc.StandardInput.Write(($pipelineInput.ToArray() -join [Environment]::NewLine))
        }
        $proc.StandardInput.Close()

        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()
        $global:LASTEXITCODE = $proc.ExitCode

        if (-not [string]::IsNullOrEmpty($stdout)) {
            Write-Output $stdout.TrimEnd()
        }
        if (-not [string]::IsNullOrEmpty($stderr)) {
            Write-Output $stderr.TrimEnd()
        }
    }
}

function Expand-FixtureText {
    param([string]$Text)
    return $Text.Replace('{{PROJECT_ROOT}}', $repoRoot.Replace('\', '/'))
}

function Get-JsonProperty {
    param(
        [object]$Object,
        [string]$Path
    )
    $current = $Object
    foreach ($segment in $Path.Split('.')) {
        if ($null -eq $current) { return $null }
        if ($segment -match '^\d+$') {
            $index = [int]$segment
            if ($current -is [System.Array] -or $current -is [Object[]]) {
                $current = $current[$index]
            }
            else {
                return $null
            }
            continue
        }
        if ($segment -match '^(\w+)\[(\d+)\]$') {
            $name = $Matches[1]
            $index = [int]$Matches[2]
            $current = $current.$name
            if ($current -is [System.Array] -or $current -is [Object[]]) {
                $current = $current[$index]
            }
            else {
                return $null
            }
        }
        elseif ($current -is [PSCustomObject]) {
            $current = $current.$segment
        }
        else {
            return $null
        }
    }
    return $current
}

function Invoke-HookCase {
    param(
        [string]$ScriptRel,
        [string]$FixtureRel
    )
    $scriptPath = Join-Path $repoRoot ($ScriptRel -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    $fixturePath = Join-Path $repoRoot ($FixtureRel -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    $stdin = Expand-FixtureText (Get-Content -LiteralPath $fixturePath -Raw -Encoding UTF8)

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $script:HookPowerShellExe
    $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.WorkingDirectory = $repoRoot
    Set-ProcessPowerShellCacheEnvironment -ProcessStartInfo $psi

    $proc = [System.Diagnostics.Process]::Start($psi)
    $proc.StandardInput.Write($stdin)
    $proc.StandardInput.Close()
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()

    return @{
        ExitCode = $proc.ExitCode
        Stdout   = $stdout.Trim()
        Stderr   = $stderr.Trim()
    }
}

function Invoke-CommandCase {
    param([string]$Command)
    $expanded = Expand-FixtureText $Command
    $global:LASTEXITCODE = 0
    $output = Invoke-Expression $expanded 2>&1 | Out-String
    return @{
        ExitCode = $global:LASTEXITCODE
        Stdout   = $output.Trim()
        Stderr   = ''
    }
}

function Test-CaseAssert {
    param(
        [object]$Assert,
        [hashtable]$Result
    )
    $errors = New-Object System.Collections.Generic.List[string]

    if ($Assert.PSObject.Properties.Name -contains 'exitCode') {
        $expected = [int]$Assert.exitCode
        if ($Result.ExitCode -ne $expected) {
            $errors.Add("exitCode expected $expected, got $($Result.ExitCode)")
        }
    }

    if ($Assert.stdoutContains) {
        foreach ($needle in @($Assert.stdoutContains)) {
            if ($Result.Stdout -notmatch [regex]::Escape($needle)) {
                $errors.Add("stdout missing: $needle")
            }
        }
    }

    if ($Assert.stderrContains) {
        foreach ($needle in @($Assert.stderrContains)) {
            if ($Result.Stderr -notmatch [regex]::Escape($needle)) {
                $errors.Add("stderr missing: $needle")
            }
        }
    }

    if ($Assert.jsonStdout) {
        try {
            $parsed = $Result.Stdout | ConvertFrom-Json
        }
        catch {
            $errors.Add('stdout is not valid JSON')
            return ,$errors
        }

        foreach ($prop in $Assert.jsonStdout.PSObject.Properties) {
            $expected = $prop.Value
            $actual = Get-JsonProperty -Object $parsed -Path $prop.Name
            if ("$actual" -ne "$expected") {
                $errors.Add("jsonStdout.$($prop.Name) expected '$expected', got '$actual'")
            }
        }
    }

    if ($Assert.json) {
        try {
            $parsed = $Result.Stdout | ConvertFrom-Json
        }
        catch {
            $errors.Add('stdout is not valid JSON for json assert')
            return ,$errors
        }
        foreach ($prop in $Assert.json.PSObject.Properties) {
            $expected = $prop.Value
            $actual = Get-JsonProperty -Object $parsed -Path $prop.Name
            if ("$actual" -ne "$expected") {
                $errors.Add("json.$($prop.Name) expected '$expected', got '$actual'")
            }
        }
    }

    return ,$errors
}

function Invoke-EvalCase {
    param([object]$Case)

    if ($Case.mode -eq 'manual' -and -not $IncludeAgent) {
        return @{
            id      = $Case.id
            layer   = $Case.layer
            status  = 'skipped'
            reason  = 'manual agent case - use -IncludeAgent'
            errors  = @()
        }
    }

    $run = $Case.run
    switch ($run.type) {
        'hook' {
            $result = Invoke-HookCase -ScriptRel $run.script -FixtureRel $run.stdinFixture
        }
        'command' {
            $result = Invoke-CommandCase -Command $run.command
        }
        default {
            return @{
                id     = $Case.id
                layer  = $Case.layer
                status = 'failed'
                errors = @("unknown run.type: $($run.type)")
            }
        }
    }

    $errors = Test-CaseAssert -Assert $Case.assert -Result $result
    return @{
        id      = $Case.id
        layer   = $Case.layer
        status  = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
        errors  = $errors
        exitCode = $result.ExitCode
    }
}

if (-not (Test-Path -LiteralPath $resultsDir)) {
    New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
}

$caseFiles = Get-ChildItem -LiteralPath $casesDir -Filter '*.json' | Sort-Object Name
$results = New-Object System.Collections.Generic.List[object]

foreach ($file in $caseFiles) {
    $case = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json

    if ($CaseId -and $case.id -ne $CaseId) { continue }
    if ($Layer -and $case.layer -ne $Layer) { continue }

    $results.Add((Invoke-EvalCase -Case $case))
}

$passed = @($results | Where-Object { $_.status -eq 'passed' }).Count
$failed = @($results | Where-Object { $_.status -eq 'failed' }).Count
$skipped = @($results | Where-Object { $_.status -eq 'skipped' }).Count

$summary = @{
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
    repoRoot  = $repoRoot
    total     = $results.Count
    passed    = $passed
    failed    = $failed
    skipped   = $skipped
    cases     = $results
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $resultsDir "run-$stamp.json") -Encoding utf8
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $resultsDir 'latest.json') -Encoding utf8

if ($Json) {
    Remove-RepoRootPowerShellCacheArtifact
    $summary | ConvertTo-Json -Depth 8
}
else {
    Write-Host ''
    Write-Host 'EventHub agent evals' -ForegroundColor Cyan
    Write-Host "  passed:  $passed"
    Write-Host "  failed:  $failed"
    Write-Host "  skipped: $skipped"
    Write-Host ''

    foreach ($r in $results) {
        $color = switch ($r.status) {
            'passed' { 'Green' }
            'failed' { 'Red' }
            default { 'DarkGray' }
        }
        Write-Host "  [$($r.status)] $($r.id)" -ForegroundColor $color
        foreach ($err in $r.errors) {
            Write-Host "         - $err" -ForegroundColor Red
        }
    }

    Write-Host ''
    Write-Host "Report: evals/results/latest.json"
    Remove-RepoRootPowerShellCacheArtifact
}

if ($failed -gt 0) {
    exit 1
}
exit 0
