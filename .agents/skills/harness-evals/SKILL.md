---
name: harness-evals
description: Create, update, and verify EventHub harness eval cases under harness/evals. Use when work touches eval cases, fixtures, eval runner behavior, harness regression evidence, manual agent cases, or runtime-orchestration eval coverage.
---

# Harness Evals

Use this skill for the eval lane of the EventHub harness. The committed eval tree is `harness/evals/`; never create a root `evals/` tree.

## Read First

Read the smallest relevant set:

1. `docs/_memory/source/harness-architecture.md`
2. `docs/_memory/source/harness-operational-policies.md`
3. `harness/manifest.json`
4. `harness/evals/README.md`
5. Existing nearby `harness/evals/cases/*.json` and `harness/evals/fixtures/*`

## Workflow

1. Identify the behavior being protected: hook, policy, graph routing, workflow skill, runtime orchestration, or manual agent behavior.
2. Prefer a deterministic case in `harness/evals/cases/`. Use `mode: manual` only when no local command can prove the behavior.
3. Add or update fixtures under `harness/evals/fixtures/` only when a case needs stable stdin or input data.
4. Keep asserts minimal: exit code plus the most meaningful JSON/stdout field.
5. Use `harness-runtime-status` when the change affects runtime contract artifacts.
6. Run the narrow case first, then the harness layer.
7. Keep `harness/evals/results/` local only; never commit result JSON.

## Case Format

Use this shape unless an existing nearby case has a stricter pattern:

```json
{
  "id": "harness-<area>-<behavior>",
  "layer": "harness",
  "mode": "auto",
  "description": "What this proves",
  "run": {
    "type": "command",
    "command": "powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1 -Json"
  },
  "assert": {
    "exitCode": 0,
    "jsonStdout": {
      "status": "passed"
    }
  }
}
```

## Naming

- Harness cases: `harness-<surface>-<behavior>.json`
- Graph cases: `graph-affected-<area>.json`
- Agent manual cases: `agent-<behavior>.json`
- Fixtures: mirror the case intent, for example `pre-tool-write-generated.json`

## Validation

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -CaseId <case-id>
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness
```

If `.graph/index.json` or `scripts/affected-tests.mjs` changed, also run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer graph
```

## Do Not

- Add root `evals/`.
- Assert broad behavior with only a README text check when a command can prove it.
- Commit `harness/evals/results/`.
- Hide eval coverage inside product tests.
