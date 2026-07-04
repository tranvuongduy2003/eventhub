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

function Test-PathAbsent {
    param([string]$RelativePath)

    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (Test-Path -LiteralPath $path) {
        Add-Error "Path must not exist: $RelativePath"
    }
}

function Test-FileContains {
    param(
        [string]$RelativePath,
        [string[]]$Needles
    )

    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Error "Missing file for content check: $RelativePath"
        return
    }

    $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    foreach ($needle in $Needles) {
        if ($text -notmatch [regex]::Escape($needle)) {
            Add-Error "$RelativePath missing required text: $needle"
        }
    }
}

foreach ($path in @(
    'AGENTS.md',
    '.agents/skills/spec/SKILL.md',
    '.agents/skills/harness-evals/SKILL.md',
    '.agents/skills/harness-orchestrator/SKILL.md',
    '.agents/skills/harness-policies/SKILL.md',
    '.agents/skills/harness-telemetry/SKILL.md',
    '.agents/skills/harness-tools/SKILL.md',
    '.agents/skills/memory-sync/SKILL.md',
    '.codex/policies/harness-policy.json',
    '.graph/index.json',
    'docs/_memory/source/harness-architecture.md',
    'docs/_memory/specs/README.md',
    'harness/manifest.json',
    'harness/orchestrator/routing.json',
    'harness/orchestrator/task-spec.schema.json',
    'harness/policies/runtime-policy.json',
    'harness/telemetry/events.schema.json',
    'harness/tools/registry.json',
    'scripts/agent/Get-HarnessStatus.ps1',
    'scripts/agent/New-HarnessSkill.ps1',
    'scripts/agent/Test-DocsMemory.ps1',
    'harness/evals/cases/harness-runtime-status.json',
    'harness/evals/cases/harness-docs-memory-lifecycle.json'
)) {
    Test-VerifyExpectation -RelativePath $path -Expected $true
}

Test-PathAbsent 'evals'
Test-PathAbsent 'harness/README.md'
Test-PathAbsent 'harness/orchestrator/README.md'
Test-PathAbsent 'harness/policies/README.md'
Test-PathAbsent 'harness/telemetry/README.md'
Test-PathAbsent 'harness/tools/README.md'

Test-FileContains '.agents/skills/spec/SKILL.md' @(
    '## 7. Harness Impact',
    'evals/',
    'harness/orchestrator/',
    'harness/telemetry/',
    'harness/tools/'
)

Test-FileContains '.agents/skills/plan/SKILL.md' @(
    '## Harness Impact',
    'evals/',
    'harness/orchestrator/',
    'harness/telemetry/',
    'harness/tools/'
)

Test-FileContains '.agents/skills/cook/SKILL.md' @(
    '## Step 1b: Harness Impact Gate',
    'harness/evals/run.ps1 -Layer harness',
    'harness/telemetry/',
    'harness/tools/'
)

Test-FileContains 'docs/_memory/source/harness-architecture.md' @(
    'Workflow Harness Contract',
    'spec',
    'plan',
    'cook',
    'Memory Sync inventory',
    'memory-sync',
    'source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data',
    'docs-memory plus changed-code verification before handoff',
    'Do not add a root `evals/` tree'
)

Test-FileContains 'harness/evals/README.md' @(
    'harness/evals/',
    'Do not add root `evals/`'
)

Test-FileContains 'AGENTS.md' @(
    'harness-evals',
    'harness-orchestrator',
    'harness-policies',
    'harness-telemetry',
    'harness-tools',
    'memory-sync',
    'memory sync explicit',
    'marks the related spec `implemented`'
)

Test-FileContains 'docs/_memory/source/harness-architecture.md' @(
    'Workflow Harness Contract',
    'spec',
    'plan',
    'cook',
    'Memory Sync inventory',
    'memory-sync',
    'source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data',
    'docs-memory plus changed-code verification before handoff',
    'Do not add a root `evals/` tree'
)


Test-FileContains 'docs/_memory/source/harness-operational-policies.md' @(
    'Memory Sync inventory',
    'related spec is marked `implemented`',
    'every affected long-term memory and harness contract surface is current',
    'changed-code verification passes'
)

Test-FileContains 'docs/_memory/mocs/harness-memory.md' @(
    'Workflow memory sync',
    'plans include a `memory-sync` inventory',
    'completed specs implemented'
)


Test-FileContains 'docs/_memory/long-term-memory-operating-model.md' @(
    'Completion memory sync',
    'Do not stop at the first obvious index',
    'source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data',
    'GitHub issue/PR status'
)

Test-FileContains 'docs/_memory/source-of-truth-map.md' @(
    'Spec completion or workflow handoff',
    'Related spec, source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data'
)

Test-FileContains 'docs/_memory/agent-retrieval-guide.md' @(
    'creates or completes durable knowledge',
    'specs, MOCs, glossaries, source maps, README/index files, harness contracts, graph/routing data'
)

Test-FileContains 'docs/README.md' @(
    'not only the most obvious MOC',
    'source docs, MOCs, glossaries, retrieval guides, indexes, README files, and harness contracts'
)

Test-FileContains 'docs/_memory/specs/README.md' @(
    'Completion Sync',
    'every affected long-term memory surface',
    'source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data'
)
Test-FileContains 'harness/orchestrator/routing.json' @(
    'requiresMemorySync',
    'requiresMemorySyncTable',
    'requiresMemorySyncGate',
    'memorySyncSkill',
    'memory-sync',
    'related spec status and all affected memory surfaces synchronized before handoff'
)

Test-FileContains 'README.md' @(
    'Harness Impact and `memory-sync` inventory',
    'mark spec implemented',
    'refresh all affected long-term memory and harness contract surfaces'
)

Test-FileContains '.agents/skills/memory-sync/SKILL.md' @(
    'name: memory-sync',
    '## Inventory',
    '## Workflow',
    '## Validation',
    '## Do Not',
    'Do not stop at the first obvious MOC'
)

Test-FileContains 'docs/_memory/source/harness-architecture.md' @(
    '## Workflow Skills',
    '`memory-sync` owns durable docs-memory completion audits',
    'workflow skill, not a runtime harness lane'
)

Test-FileContains 'docs/_memory/source/harness-operational-policies.md' @(
    '`memory-sync` for completion audits',
    'workflow skill, not a runtime lane'
)

Test-FileContains 'scripts/agent/Get-HarnessStatus.ps1' @(
    'memorySyncSkill must be memory-sync',
    'Missing memory-sync skill'
)
Test-FileContains 'scripts/agent/New-HarnessSkill.ps1' @(
    'ValidateSet(''evals'', ''orchestrator'', ''policies'', ''telemetry'', ''tools'')',
    '.agents\skills\',
    'harness/evals/'
)

Test-FileContains 'scripts/agent/Get-HarnessStatus.ps1' @(
    'harness/manifest.json',
    'harness/orchestrator/routing.json',
    'harness/policies/runtime-policy.json',
    'harness/telemetry/events.schema.json',
    'harness/tools/registry.json',
    'Forbidden placeholder path exists'
)

Test-FileContains 'harness/manifest.json' @(
    '"statusCommand"',
    '"evalCommand"',
    '"orchestrator"',
    '"policies"',
    '"telemetry"',
    '"tools"'
)

foreach ($skill in @(
    'harness-evals',
    'harness-orchestrator',
    'harness-policies',
    'harness-telemetry',
    'harness-tools'
)) {
    Test-FileContains ".agents/skills/$skill/SKILL.md" @(
        "name: $skill",
        '## Read First',
        '## Workflow',
        '## Validation',
        '## Do Not'
    )
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
