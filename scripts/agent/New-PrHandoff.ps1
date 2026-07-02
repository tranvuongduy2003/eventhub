#Requires -Version 5.1
<#
.SYNOPSIS
  Produce a reviewable handoff from the current working tree.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Invoke-GitLines {
    param([string[]]$ArgumentList)
    Push-Location $repoRoot
    try {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        return ,@(& git @ArgumentList 2>$null)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        Pop-Location
    }
}

$changed = New-Object System.Collections.Generic.List[string]
foreach ($line in (Invoke-GitLines -ArgumentList @('diff', '--name-only'))) {
    if ($line) { $changed.Add([string]$line) }
}
foreach ($line in (Invoke-GitLines -ArgumentList @('diff', '--name-only', '--cached'))) {
    if ($line) { $changed.Add([string]$line) }
}
foreach ($line in (Invoke-GitLines -ArgumentList @('ls-files', '--others', '--exclude-standard'))) {
    if ($line) { $changed.Add([string]$line) }
}

$files = @($changed | Select-Object -Unique)
$verifyReportPath = Join-Path $repoRoot '.codex\state\verify-changed-code-latest.json'
$latestEval = Join-Path $repoRoot 'evals\results\latest.json'
$verifySummary = $null
if (Test-Path -LiteralPath $verifyReportPath) {
    try {
        $verify = Get-Content -LiteralPath $verifyReportPath -Raw | ConvertFrom-Json
        $verifySummary = "changed-code verification: status=$($verify.status); commands=$(@($verify.commands).Count); errors=$(@($verify.errors).Count)"
    }
    catch {
        $verifySummary = $null
    }
}

$evalSummary = $null
if (Test-Path -LiteralPath $latestEval) {
    try {
        $eval = Get-Content -LiteralPath $latestEval -Raw | ConvertFrom-Json
        $evalSummary = "latest eval layer: passed=$($eval.passed), failed=$($eval.failed), skipped=$($eval.skipped)"
    }
    catch {
        $evalSummary = $null
    }
}

$verificationLines = @()
if ($verifySummary) { $verificationLines += $verifySummary }
if ($evalSummary) { $verificationLines += $evalSummary }
if ($verificationLines.Count -eq 0) {
    $verificationLines += 'Add commands run before handoff.'
}

$template = New-Object System.Collections.Generic.List[string]
$template.Add('## Summary')
$template.Add('- ')
$template.Add('')
$template.Add('## Changed files')
foreach ($file in $files) {
    $template.Add("- $file")
}
$template.Add('')
$template.Add('## Verification')
foreach ($line in $verificationLines) {
    $template.Add("- $line")
}
$template.Add('')
$template.Add('## Risks and reviewer focus')
$template.Add('- ')

$result = @{
    files = $files
    verification = $verificationLines
    template = $template.ToArray()
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
    exit 0
}

$result.template | ForEach-Object { Write-Output $_ }
exit 0
