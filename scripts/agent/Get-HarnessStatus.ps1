#Requires -Version 5.1
<#
.SYNOPSIS
  Validate and summarize the EventHub harness runtime contract artifacts.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$errors = New-Object System.Collections.Generic.List[string]

function Add-Error {
    param([string]$Message)
    $script:errors.Add($Message) | Out-Null
}

function Get-RelativePath {
    param([string]$Path)
    return ($Path -replace '/', '\')
}

function Test-RequiredFile {
    param([string]$RelativePath)

    $path = Join-Path $repoRoot (Get-RelativePath $RelativePath)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Error "Missing required file: $RelativePath"
        return $false
    }
    return $true
}

function Test-ForbiddenPath {
    param([string]$RelativePath)

    $path = Join-Path $repoRoot (Get-RelativePath $RelativePath)
    if (Test-Path -LiteralPath $path) {
        Add-Error "Forbidden placeholder path exists: $RelativePath"
    }
}

function Read-Json {
    param([string]$RelativePath)

    if (-not (Test-RequiredFile -RelativePath $RelativePath)) {
        return $null
    }

    try {
        return Get-Content -LiteralPath (Join-Path $repoRoot (Get-RelativePath $RelativePath)) -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        Add-Error "Invalid JSON in $RelativePath`: $($_.Exception.Message)"
        return $null
    }
}

$forbiddenReadmes = @(
    'evals',
    'harness/README.md',
    'harness/orchestrator/README.md',
    'harness/policies/README.md',
    'harness/telemetry/README.md',
    'harness/tools/README.md'
)

foreach ($path in $forbiddenReadmes) {
    Test-ForbiddenPath -RelativePath $path
}

$manifest = Read-Json 'harness/manifest.json'
$routing = Read-Json 'harness/orchestrator/routing.json'
$taskSpecSchema = Read-Json 'harness/orchestrator/task-spec.schema.json'
$runtimePolicy = Read-Json 'harness/policies/runtime-policy.json'
$telemetrySchema = Read-Json 'harness/telemetry/events.schema.json'
$toolRegistry = Read-Json 'harness/tools/registry.json'

$expectedLanes = @('evals', 'orchestrator', 'policies', 'telemetry', 'tools')
$laneResults = New-Object System.Collections.ArrayList

if ($null -ne $manifest) {
    if ($manifest.singleEvalTree -ne 'harness/evals/') {
        Add-Error "harness/manifest.json singleEvalTree must be harness/evals/"
    }

    foreach ($lane in $expectedLanes) {
        $laneInfo = $manifest.lanes.$lane
        if ($null -eq $laneInfo) {
            Add-Error "harness/manifest.json missing lane: $lane"
            continue
        }

        $artifactStatuses = New-Object System.Collections.ArrayList
        foreach ($artifact in @($laneInfo.artifacts)) {
            $artifactPath = Join-Path $repoRoot (Get-RelativePath $artifact)
            $exists = Test-Path -LiteralPath $artifactPath
            if (-not $exists) {
                Add-Error "Lane $lane missing artifact: $artifact"
            }
            $artifactStatuses.Add([pscustomobject]@{
                path = $artifact
                exists = [bool]$exists
            }) | Out-Null
        }

        $laneResults.Add([pscustomobject]@{
            lane = $lane
            skill = $laneInfo.skill
            root = $laneInfo.root
            artifacts = @($artifactStatuses)
        }) | Out-Null
    }
}

if ($null -ne $routing) {
    foreach ($skill in @('harness-evals', 'harness-orchestrator', 'harness-policies', 'harness-telemetry', 'harness-tools')) {
        $found = $false
        foreach ($prop in $routing.workflows.'harness-lane-change'.laneSkills.PSObject.Properties) {
            if ($prop.Value -eq $skill) {
                $found = $true
                break
            }
        }
        if (-not $found) {
            Add-Error "harness/orchestrator/routing.json missing lane skill: $skill"
        }
    }
}

if ($null -ne $taskSpecSchema) {
    foreach ($required in @('id', 'objective', 'workflow', 'harnessImpact', 'stopConditions')) {
        if (@($taskSpecSchema.required) -notcontains $required) {
            Add-Error "task-spec.schema.json missing required field: $required"
        }
    }
}

if ($null -ne $runtimePolicy) {
    foreach ($path in @('web/src/generated/', 'contracts/openapi/.build/', '.env', '.mcp.json')) {
        if (@($runtimePolicy.protectedEditPaths) -notcontains $path) {
            Add-Error "runtime-policy.json missing protected path: $path"
        }
    }
}

if ($null -ne $telemetrySchema) {
    foreach ($eventName in @('harness.eval.completed', 'harness.policy.decision', 'harness.status.checked')) {
        if ($null -eq $telemetrySchema.events.$eventName) {
            Add-Error "events.schema.json missing event: $eventName"
        }
    }
}

if ($null -ne $toolRegistry) {
    foreach ($toolName in @('verify-changed-code', 'test-harness-policy', 'get-harness-status', 'new-harness-skill', 'eval-runner')) {
        if ($null -eq $toolRegistry.tools.$toolName) {
            Add-Error "registry.json missing tool: $toolName"
        }
    }
}

$result = @{
    status = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    lanes = @($laneResults)
    errors = @($errors)
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

if ($Json) {
    $result | ConvertTo-Json -Depth 10
}
else {
    Write-Host ''
    Write-Host 'EventHub harness status' -ForegroundColor Cyan
    Write-Host "  status: $($result.status)"
    foreach ($lane in $laneResults) {
        $present = @($lane.artifacts | Where-Object { $_.exists }).Count
        $total = @($lane.artifacts).Count
        Write-Host "  $($lane.lane): $present/$total artifacts"
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
