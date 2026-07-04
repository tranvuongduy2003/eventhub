#Requires -Version 5.1
<#
.SYNOPSIS
  Validate cook workflow artifacts and deterministic dry-run contracts.
#>

param(
    [string]$PlanPath,
    [string]$ProgressPath,
    [string]$TaskSpecPath,
    [string]$FeatureId,
    [switch]$DryRun,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$errors = New-Object System.Collections.Generic.List[string]

function Add-Error {
    param([string]$Message)
    $script:errors.Add($Message) | Out-Null
}

function Resolve-RepoPath {
    param([string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return Join-Path $repoRoot ($Path -replace '/', '\')
}

function Read-RequiredText {
    param(
        [string]$Path,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        Add-Error "$Label path is required"
        return $null
    }

    $resolved = Resolve-RepoPath $Path
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        Add-Error "$Label does not exist: $Path"
        return $null
    }

    return Get-Content -LiteralPath $resolved -Raw -Encoding UTF8
}

function Test-Heading {
    param(
        [string]$Text,
        [string]$Heading,
        [string]$Label
    )

    if ($Text -notmatch "(?m)^##\s+$([regex]::Escape($Heading))\s*$") {
        Add-Error "$Label missing section: ## $Heading"
    }
}

function Test-Contains {
    param(
        [string]$Text,
        [string]$Needle,
        [string]$Label
    )

    if ($Text -notmatch [regex]::Escape($Needle)) {
        Add-Error "$Label missing required text: $Needle"
    }
}

function Get-FeatureBlock {
    param([string]$Id)

    $featureSpecPath = Join-Path $repoRoot 'docs\_memory\source\feature-specification.md'
    if (-not (Test-Path -LiteralPath $featureSpecPath -PathType Leaf)) {
        Add-Error 'Feature specification source is missing'
        return $null
    }

    $text = Get-Content -LiteralPath $featureSpecPath -Raw -Encoding UTF8
    $escaped = [regex]::Escape($Id)
    $match = [regex]::Match($text, "(?ms)^####\s+$escaped\b.*?(?=^####\s+F-\d+\.\d+\b|^##\s+EP-|\z)")
    if (-not $match.Success) {
        Add-Error "Feature id not found in source feature specification: $Id"
        return $null
    }

    return $match.Value
}

function Test-Plan {
    param([string]$Text)

    foreach ($heading in @('Harness Impact', 'Memory Sync Inventory', 'Tasks', 'Done Criteria Ledger')) {
        Test-Heading -Text $Text -Heading $heading -Label 'Plan'
    }

    if ($Text -match '\bF-\d+\.\d+\b' -or -not [string]::IsNullOrWhiteSpace($FeatureId)) {
        Test-Heading -Text $Text -Heading 'Adjacent Feature Boundary' -Label 'Plan'
    }

    foreach ($lane in @('evals', 'orchestrator', 'policies', 'telemetry', 'tools', 'workflow')) {
        if ($Text -notmatch "(?im)^\|\s*$lane\s*\|") {
            Add-Error "Plan Harness Impact table missing lane: $lane"
        }
    }

    foreach ($surface in @('Related spec', 'Source docs', 'MOCs/glossaries/retrieval guides', 'README/index files', 'Harness contracts and graph/routing', 'External tracking')) {
        if ($Text -notmatch [regex]::Escape($surface)) {
            Add-Error "Plan Memory Sync inventory missing surface: $surface"
        }
    }

    if ($Text -notmatch '(?m)^-\s+\[[ xX]\]\s+') {
        Add-Error 'Plan tasks must use markdown checkboxes'
    }

    if ($Text -notmatch '(powershell|dotnet|yarn|node)\s+') {
        Add-Error 'Plan must include at least one validation command'
    }

    if ($Text -notmatch 'scripts/agent/Verify-ChangedCode.ps1') {
        Add-Error 'Plan Done Criteria Ledger must include changed-code verification'
    }
}

function Test-Progress {
    param([string]$Text)

    foreach ($heading in @('Decisions', 'Changed Files', 'Blockers', 'Next Steps', 'Verification')) {
        if ($Text -notmatch "(?m)^##\s+$([regex]::Escape($heading))\s*$") {
            Add-Error "Progress note missing section: ## $heading"
        }
    }
}

function Test-TaskSpec {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $text = Read-RequiredText -Path $Path -Label 'TaskSpec'
    if ($null -eq $text) {
        return
    }

    try {
        $task = $text | ConvertFrom-Json
    }
    catch {
        Add-Error "TaskSpec is not valid JSON: $($_.Exception.Message)"
        return
    }

    foreach ($field in @('id', 'objective', 'workflow', 'phase', 'harnessImpact', 'memorySyncInventory', 'stopConditions', 'doneCriteriaLedger')) {
        if ($null -eq $task.$field) {
            Add-Error "TaskSpec missing required field: $field"
        }
    }

    foreach ($lane in @('evals', 'orchestrator', 'policies', 'telemetry', 'tools', 'workflow')) {
        if ($null -eq $task.harnessImpact.$lane) {
            Add-Error "TaskSpec harnessImpact missing lane: $lane"
        }
    }
}

$dryRunResult = $null
if ($DryRun) {
    if (-not [string]::IsNullOrWhiteSpace($FeatureId)) {
        $block = Get-FeatureBlock -Id $FeatureId
        $hasDependency = $block -match 'depends on'
        $hasAcceptance = $block -match 'Acceptance criteria'
        $dryRunResult = @{
            featureId = $FeatureId
            createsArtifacts = $false
            intendedArtifacts = @(
                'docs/_memory/specs/<timestamp>-<slug>.md',
                '.codex/plans/<same-filename-as-spec>.md',
                '.codex/notes/progress.md',
                '.codex/state/cook/<task-id>.json'
            )
            requiresAdjacentFeatureBoundary = [bool]$hasDependency
            recordsAcceptanceCriteria = [bool]$hasAcceptance
        }
    }
    else {
        $dryRunResult = @{
            featureId = $null
            createsArtifacts = $false
            intendedArtifacts = @(
                'docs/_memory/specs/<timestamp>-<slug>.md',
                '.codex/plans/<same-filename-as-spec>.md',
                '.codex/notes/progress.md',
                '.codex/state/cook/<task-id>.json'
            )
            requiresAdjacentFeatureBoundary = $true
            recordsAcceptanceCriteria = $false
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($PlanPath)) {
    $planText = Read-RequiredText -Path $PlanPath -Label 'Plan'
    if ($null -ne $planText) {
        Test-Plan -Text $planText
    }
}

if (-not [string]::IsNullOrWhiteSpace($ProgressPath)) {
    $progressText = Read-RequiredText -Path $ProgressPath -Label 'Progress note'
    if ($null -ne $progressText) {
        Test-Progress -Text $progressText
    }
}

Test-TaskSpec -Path $TaskSpecPath

$result = @{
    status = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    errors = @($errors)
    dryRun = $dryRunResult
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

if ($Json) {
    $result | ConvertTo-Json -Depth 8
}
else {
    Write-Host ''
    Write-Host 'EventHub cook artifact validation' -ForegroundColor Cyan
    Write-Host "  status: $($result.status)"
    if ($DryRun -and $null -ne $dryRunResult) {
        Write-Host "  feature: $FeatureId"
        Write-Host "  creates artifacts: $($dryRunResult.createsArtifacts)"
    }
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
