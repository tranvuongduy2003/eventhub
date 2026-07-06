#Requires -Version 5.1
<#
.SYNOPSIS
  Create or update a repo-local EventHub harness skill from a standard lane template.
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('evals', 'orchestrator', 'policies', 'telemetry', 'tools')]
    [string]$Lane,

    [switch]$Force,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$definitions = @{
    evals = @{
        name = 'harness-evals'
        display = 'Harness Evals'
        short = 'Create and maintain EventHub harness eval cases.'
        prompt = 'Use harness-evals to add or update EventHub eval cases and verification evidence.'
        description = 'Create, update, and verify EventHub harness eval cases under harness/evals. Use when work touches eval cases, fixtures, eval runner behavior, harness regression evidence, manual agent cases, or runtime-orchestration eval coverage.'
    }
    orchestrator = @{
        name = 'harness-orchestrator'
        display = 'Harness Orchestrator'
        short = 'Design EventHub harness orchestration runtime contracts.'
        prompt = 'Use harness-orchestrator to plan or update EventHub harness runtime orchestration.'
        description = 'Design or update EventHub harness orchestration runtime contracts under harness/orchestrator. Use when work touches TaskSpec routing, agent selection, handoffs, retries, approval flow, stop conditions, Codex-as-executor, Responses API, Agents SDK, or runtime orchestration boundaries.'
    }
    policies = @{
        name = 'harness-policies'
        display = 'Harness Policies'
        short = 'Maintain EventHub harness policy and guardrail rules.'
        prompt = 'Use harness-policies to update EventHub permission, approval, guardrail, or protected-path policy.'
        description = 'Create, update, and verify EventHub harness policies and guardrails. Use when work touches .codex/policies/harness-policy.json, harness/policies, protected paths, dangerous command rules, approval policy, permission mapping, hook enforcement data, or policy eval coverage.'
    }
    telemetry = @{
        name = 'harness-telemetry'
        display = 'Harness Telemetry'
        short = 'Design EventHub harness traces, logs, metrics, and evidence.'
        prompt = 'Use harness-telemetry to add or update EventHub harness observability and improvement-loop evidence.'
        description = 'Design or update EventHub harness telemetry, traces, logs, metrics, run evidence, and improvement-loop records. Use when work touches harness/telemetry, eval result evidence, tool-call records, guardrail events, trace schemas, or monitoring-to-evals feedback loops.'
    }
    tools = @{
        name = 'harness-tools'
        display = 'Harness Tools'
        short = 'Create EventHub harness tool adapter and CLI contracts.'
        prompt = 'Use harness-tools to create or update EventHub hosted tool, MCP, or CLI adapter contracts.'
        description = 'Create, update, and verify EventHub harness tool adapters and agent-friendly CLI contracts. Use when work touches harness/tools, MCP adapters, hosted tool adapters, local CLI wrappers, scripts used by skills, read/write command separation, JSON output contracts, or tool eval coverage.'
    }
}

$definition = $definitions[$Lane]
$skillRoot = Join-Path $repoRoot ".agents\skills\$($definition.name)"
$agentsRoot = Join-Path $skillRoot 'agents'
$skillPath = Join-Path $skillRoot 'SKILL.md'
$openAiPath = Join-Path $agentsRoot 'openai.yaml'

if ((Test-Path -LiteralPath $skillRoot) -and -not $Force) {
    $result = @{
        status = 'skipped'
        reason = 'skill exists; pass -Force to overwrite from the standard skeleton'
        lane = $Lane
        path = $skillRoot
    }
}
else {
    New-Item -ItemType Directory -Force -Path $agentsRoot | Out-Null

    $body = @"
---
name: $($definition.name)
description: $($definition.description)
---

# $($definition.display)

Use this skill for the $Lane lane of the EventHub harness.

## Read First

Read the smallest relevant set:

1. ``docs/_memory/source/harness-architecture.md``
2. ``docs/_memory/source/harness-operational-policies.md``
3. ``harness/manifest.json``
4. Existing nearby JSON contract, skill, script, policy, or eval files

## Workflow

1. Identify the concrete harness behavior being changed.
2. Keep the lane boundary explicit in the plan Harness Impact table.
3. Change the smallest owning artifact.
4. Add or update objective eval coverage when behavior changes.
5. Run ``powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/Invoke-HarnessEvals.ps1 -Layer harness``.

## Standard Sections

Use these sections for lane design notes:

````markdown
## Purpose
## Contract
## Inputs
## Outputs
## Validation
## Evals
````

## Do Not

- Mix EventHub product behavior into harness runtime artifacts.
- Create root ``evals/``.
- Leave harness impact implicit.
"@

    Set-Content -LiteralPath $skillPath -Value $body -Encoding UTF8

    $openAi = @"
interface:
  display_name: "$($definition.display)"
  short_description: "$($definition.short)"
  default_prompt: "$($definition.prompt)"
"@
    Set-Content -LiteralPath $openAiPath -Value $openAi -Encoding UTF8

    $result = @{
        status = 'updated'
        lane = $Lane
        skill = $definition.name
        path = $skillRoot
    }
}

if ($Json) {
    $result | ConvertTo-Json -Depth 5
}
else {
    Write-Host "EventHub harness skill scaffold"
    Write-Host "  status: $($result.status)"
    Write-Host "  lane: $Lane"
    Write-Host "  path: $skillRoot"
    if ($result.reason) {
        Write-Host "  reason: $($result.reason)"
    }
}
