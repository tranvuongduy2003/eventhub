# Agent eval pipeline

Objective evidence for harness / graph / agent changes, including future runtime-orchestration checks from `harness/` - not gut feel.

## Layout

```
harness/evals/
  run.ps1          # entry (pwsh or Windows PowerShell)
  run              # bash → pwsh (CI / Git Bash)
  cases/*.json     # input + assert per scenario, including runtime harness cases
  fixtures/        # hook stdin payloads (use {{PROJECT_ROOT}})
  results/         # latest.json + timestamped runs (gitignored)
```

## Case schema

```json
{
  "id": "harness-pre-tool-block-generated",
  "layer": "harness | graph | agent",
  "mode": "auto | manual",
  "description": "what this proves",
  "run": {
    "type": "hook | command",
    "script": ".codex/hooks/pre-tool-guard.ps1",
    "stdinFixture": "harness/evals/fixtures/pre-tool-write-generated.json"
  },
  "assert": {
    "exitCode": 2,
    "json": { "permission": "deny" },
    "jsonStdout": { "skip": false, "steps.1.kind": "dotnet-test" },
    "stdoutContains": ["generated"]
  }
}
```

## Run

```powershell
.\harness\evals\run.ps1
.\harness\evals\run.ps1 -Layer harness
.\harness\evals\run.ps1 -CaseId graph-affected-domain-users
.\harness\evals\run.ps1 -Json
```

Manual **agent** cases (`mode: manual`) define prompts + post-conditions for Codex sessions; skipped unless `-IncludeAgent`.

## When to run

| Change | Run |
|--------|-----|
| `.codex/hooks/**`, `hooks.json` | `.\harness\evals\run.ps1 -Layer harness` |
| `harness/orchestrator/**`, `harness/policies/**`, `harness/telemetry/**`, `harness/tools/**` | `.\harness\evals\run.ps1 -Layer harness` |
| `harness/manifest.json`, runtime contract artifacts | `.\harness\evals\run.ps1 -CaseId harness-runtime-status` |
| `harness/graph/index.json`, `affected-tests.ps1` | `.\harness\evals\run.ps1 -Layer graph` |
| Rules / prompts / agent defs | full suite + manual agent cases; invoke `code-reviewer` subagent after substantial edits |
| Before merging harness PR | CI job `agent-evals` |

## Adding a case

1. Reproduce the regression or desired behavior as a **deterministic** check when possible.
2. Add fixture + case JSON; keep asserts minimal (exit code + one JSON field).
3. Run `.\harness\evals\run.ps1 -CaseId <new-id>` until green.
4. Commit case + fixture; results stay local.

## Layers

| Layer | What it validates |
|-------|-------------------|
| **harness** | Hook scripts block/allow correctly |
| **graph** | `affected-tests.ps1` + `harness/graph/index.json` mapping |
| **agent** | End-to-end agent behavior (manual or future SDK runner) |

`harness/evals/` is the only committed eval tree. Do not add root `evals/`; runtime-orchestration evals live here so repo-level and runtime-level harness evidence share one runner and result format.

See also: [`docs/_memory/source/harness-architecture.md`](../../docs/_memory/source/harness-architecture.md) and [`docs/_memory/source/harness-operational-policies.md`](../../docs/_memory/source/harness-operational-policies.md).
