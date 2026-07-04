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
    if ($manifest.repoGuidance -ne 'AGENTS.md') {
        Add-Error "harness/manifest.json repoGuidance must be AGENTS.md"
    }
    elseif (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'AGENTS.md') -PathType Leaf)) {
        Add-Error "Missing repo guidance: AGENTS.md"
    }

    if ($manifest.stateDirectory -ne '.codex/state') {
        Add-Error "harness/manifest.json stateDirectory must be .codex/state"
    }

    if ($manifest.singleEvalTree -ne 'harness/evals/') {
        Add-Error "harness/manifest.json singleEvalTree must be harness/evals/"
    }

    $expectedFoundationSkills = @{
        repoBootstrap = '.agents/skills/repo-bootstrap/SKILL.md'
        verifyChangedCode = '.agents/skills/verify-changed-code/SKILL.md'
        prHandoff = '.agents/skills/pr-handoff/SKILL.md'
    }

    foreach ($entry in $expectedFoundationSkills.GetEnumerator()) {
        $actual = [string]$manifest.foundationSkills.($entry.Key)
        if ($actual -ne $entry.Value) {
            Add-Error "harness/manifest.json foundationSkills.$($entry.Key) must be $($entry.Value)"
            continue
        }

        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot (Get-RelativePath $entry.Value)) -PathType Leaf)) {
            Add-Error "Missing foundation skill: $($entry.Value)"
        }
    }

    $expectedCommands = @{
        bootstrap = 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Repo-Bootstrap.ps1'
        verifyChanged = 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1'
        handoff = 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/New-PrHandoff.ps1'
        evalHarness = 'powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness'
    }

    foreach ($entry in $expectedCommands.GetEnumerator()) {
        if ([string]$manifest.commands.($entry.Key) -ne $entry.Value) {
            Add-Error "harness/manifest.json commands.$($entry.Key) must be $($entry.Value)"
        }
    }

    if ($manifest.futureRuntime.providerContract -ne 'Responses API') {
        Add-Error "harness/manifest.json futureRuntime.providerContract must be Responses API"
    }

    if ($manifest.futureRuntime.orchestration -ne 'Agents SDK') {
        Add-Error "harness/manifest.json futureRuntime.orchestration must be Agents SDK"
    }

    if ($manifest.futureRuntime.codexExecutor -ne 'Codex CLI via MCP when multi-step coding workflows need external orchestration') {
        Add-Error "harness/manifest.json futureRuntime.codexExecutor has an unsupported value"
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
    if ($routing.defaultWorkflow -ne 'cook-unified') {
        Add-Error "harness/orchestrator/routing.json defaultWorkflow must be cook-unified"
    }

    if ($routing.canonicalSkill -ne 'cook') {
        Add-Error "harness/orchestrator/routing.json canonicalSkill must be cook"
    }
    elseif (-not (Test-Path -LiteralPath (Join-Path $repoRoot '.agents\skills\cook\SKILL.md') -PathType Leaf)) {
        Add-Error "Missing cook skill: .agents/skills/cook/SKILL.md"
    }

    if ($routing.memorySyncSkill -ne 'memory-sync') {
        Add-Error "harness/orchestrator/routing.json memorySyncSkill must be memory-sync"
    }
    elseif (-not (Test-Path -LiteralPath (Join-Path $repoRoot '.agents\skills\memory-sync\SKILL.md') -PathType Leaf)) {
        Add-Error "Missing memory-sync skill: .agents/skills/memory-sync/SKILL.md"
    }

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

    $cookPhases = @($routing.workflows.'cook-unified'.phases | ForEach-Object { $_.name })
    if ($cookPhases -notcontains 'audit') {
        Add-Error "harness/orchestrator/routing.json cook-unified phases missing audit"
    }

    $planPhase = $routing.workflows.'cook-unified'.phases | Where-Object { $_.name -eq 'plan' } | Select-Object -First 1
    if ($null -eq $planPhase.validator -or [string]$planPhase.validator -notmatch 'scripts/agent/Test-CookPlan.ps1') {
        Add-Error "harness/orchestrator/routing.json plan phase must reference Test-CookPlan.ps1"
    }
}

if ($null -ne $taskSpecSchema) {
    foreach ($required in @('id', 'objective', 'workflow', 'phase', 'harnessImpact', 'stopConditions')) {
        if (@($taskSpecSchema.required) -notcontains $required) {
            Add-Error "task-spec.schema.json missing required field: $required"
        }
    }

    foreach ($workflow in @($taskSpecSchema.properties.workflow.enum)) {
        if ($workflow -ne 'cook' -and $workflow -ne 'harness') {
            Add-Error "task-spec.schema.json workflow enum contains unsupported workflow: $workflow"
        }
    }

    foreach ($phase in @('spec', 'plan', 'implement', 'verify', 'handoff')) {
        if (@($taskSpecSchema.properties.phase.enum) -notcontains $phase) {
            Add-Error "task-spec.schema.json phase enum missing cook phase: $phase"
        }
    }

    if (@($taskSpecSchema.properties.phase.enum) -notcontains 'audit') {
        Add-Error "task-spec.schema.json phase enum missing cook audit phase"
    }

    foreach ($required in @('memorySyncInventory', 'doneCriteriaLedger')) {
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
    foreach ($toolName in @('verify-changed-code', 'test-harness-policy', 'get-harness-status', 'test-cook-plan', 'new-harness-skill', 'eval-runner')) {
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
