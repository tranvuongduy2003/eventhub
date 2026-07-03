---
name: harness-telemetry
description: Design or update EventHub harness telemetry, traces, logs, metrics, run evidence, and improvement-loop records. Use when work touches harness/telemetry, eval result evidence, tool-call records, guardrail events, trace schemas, or monitoring-to-evals feedback loops.
---

# Harness Telemetry

Use this skill for the telemetry lane of the EventHub harness. Telemetry is evidence for improvement, not decoration.

## Read First

Read the smallest relevant set:

1. `docs/_memory/source/harness-architecture.md`
2. `docs/_memory/source/harness-operational-policies.md`
3. `harness/telemetry/events.schema.json`
4. `harness/evals/README.md`
5. `scripts/agent/New-PrHandoff.ps1` when handoff evidence changes

## Telemetry Contract

Every telemetry change must state:

- Event or span name.
- Producer: hook, script, orchestrator, tool adapter, or eval runner.
- Fields: required, optional, and redacted.
- Storage: committed docs, ignored state, eval results, or external trace backend.
- Retention: durable source memory, temporary task memory, or rebuildable state.
- Consumer: handoff, eval, policy decision, review, or future dashboard.

## Workflow

1. Start with the question the telemetry must answer.
2. Prefer structured JSON for machine-read evidence.
3. Keep secrets, tokens, and raw private payloads out of committed telemetry.
4. Connect telemetry to an improvement loop: trace -> feedback -> eval -> harness change.
5. Add eval coverage when telemetry affects verification or handoff behavior.

## Standard Artifact Sections

Use these fields or equivalent sections for telemetry schema updates:

```markdown
## Purpose
## Producers
## Schema
## Storage
## Redaction
## Consumers
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

- Commit `harness/evals/results/` or `.codex/state/` runtime output.
- Treat logs as sufficient unless they feed a decision, eval, or review.
- Store secrets or unsanitized tool payloads.
- Invent a second telemetry location when `harness/telemetry/` or ignored state already fits.
