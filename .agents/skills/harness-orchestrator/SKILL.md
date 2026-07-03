---
name: harness-orchestrator
description: Design or update EventHub harness orchestration runtime contracts under harness/orchestrator. Use when work touches TaskSpec routing, agent selection, handoffs, retries, approval flow, stop conditions, Codex-as-executor, Responses API, Agents SDK, or runtime orchestration boundaries.
---

# Harness Orchestrator

Use this skill for the orchestration lane of the EventHub harness. Runtime work belongs under `harness/orchestrator/` and must not contain EventHub product behavior.

## Read First

Read the smallest relevant set:

1. `docs/_memory/source/harness-architecture.md`
2. `docs/_memory/source/harness-operational-policies.md`
3. `harness/manifest.json`
4. `harness/orchestrator/routing.json`
5. `harness/orchestrator/task-spec.schema.json`
6. `harness/evals/README.md` when adding or updating orchestration evidence

## Contract

An orchestrator change must state:

- Input contract: task shape, required fields, and source of instructions.
- Routing contract: how agents, skills, and tools are selected.
- State contract: what is persisted, where it lives, and whether it is committed or ignored.
- Approval contract: which actions require user approval or policy checks.
- Stop contract: when a run is complete, blocked, retried, or handed off.
- Trace contract: which telemetry spans or events prove the run.
- Eval contract: which `harness/evals/` case protects the behavior.

## Workflow

1. Start from the desired run behavior, not from a generic framework skeleton.
2. Keep orchestration contracts separate from tool adapters, policy data, and telemetry schemas.
3. Prefer JSON contract updates until implementation is needed.
4. When implementation is needed, add the smallest runtime scaffold under `harness/orchestrator/`.
5. Add or update `harness/evals/` coverage for new routing, approval, retry, handoff, or stop behavior.
6. Update `spec` / `plan` / `cook` Harness Impact when the workflow contract changes.

## Standard Artifact Sections

Use these fields or equivalent sections for orchestration contract updates:

```markdown
## Purpose
## Inputs
## Routing
## State
## Approvals
## Stop Conditions
## Telemetry
## Evals
```

## Validation

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Get-HarnessStatus.ps1 -Json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

## Do Not

- Add product use cases or domain logic under `harness/`.
- Start new runtime work on Assistants API.
- Create root `evals/`; runtime evals live in `harness/evals/`.
- Mix policy decisions into orchestrator code when they belong in policy files.
