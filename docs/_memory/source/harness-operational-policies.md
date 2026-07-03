---
title: EventHub Harness Operational Policies
type: source
status: active
tags:
  - harness
  - source
  - operations
---

# EventHub Harness Operational Policies

## Permission Boundary

Default local autonomy is workspace-write through `.codex/config.toml`.

Protected paths are also enforced by hooks:

- `.env`, `.env.*`
- `.mcp.json` (legacy/local only; shared MCP config lives in `.codex/config.toml`)
- `web/src/generated/`
- `contracts/openapi/.build/`
- dependency and build outputs

Shared policy data lives in `.codex/policies/harness-policy.json`.

## Lifecycle Hooks

| Hook | Script | Purpose |
|---|---|---|
| PreToolUse | `.codex/hooks/pre-tool-guard.ps1` | Deny protected edits, dangerous shell commands, or gated tools |
| PreToolUse Bash | `.codex/hooks/before-shell-guard.ps1` | Fast shell-only command guard |
| PostToolUse | `.codex/hooks/post-edit-verify.ps1` | Run affected checks for edited files and set verify gate on failure |
| Stop | `.codex/hooks/stop-gate.ps1` | Block final handoff while gate or changed-file checks fail |

Hooks should stay thin. Move policy data to `.codex/policies/` and reusable command logic to `scripts/agent/` or `.codex/hooks/lib/`.

## Verification

Use this order:

1. `scripts/agent/Verify-ChangedCode.ps1 -PlanOnly`
2. `scripts/agent/Verify-ChangedCode.ps1`
3. Broader checks only when the blast radius demands it

For hook, graph, or agent harness changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness
```

For `.graph/index.json` or `scripts/affected-tests.mjs` changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer graph
```

## State And Memory

Committed:

- source-of-truth docs
- `docs/` Obsidian memory maps, glossaries, templates, and vault config
- skills
- hook scripts
- policy files
- eval cases and fixtures

Ignored:

- `.codex/state/`
- `.codex/plans/`
- `.codex/notes/progress.md`
- `.codex/agent-memory/*.md`
- `harness/evals/results/`

Do not place durable policy in ignored state files.

Validate the long-term docs memory lifecycle with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1
```

## Improvement Loop

When the harness fails or blocks incorrectly:

1. Capture the failing command or hook fixture.
2. Add or adjust an eval case.
3. Change the smallest layer that owns the failure:
   - `AGENTS.md` for working agreement
   - skill for workflow routing
   - script for execution surface
   - policy for guardrail data
   - hook for lifecycle handling
   - graph for verification scope
4. Run the relevant eval layer.

## Spec Plan Cook Contract

Harness impact is part of normal delivery, not an afterthought:

- `spec` must include a Harness Impact section and state `N/A` only when evals, orchestrator, policies, telemetry, tools, hooks, skills, scripts, graph, and AGENTS.md are unaffected.
- `plan` must include a Harness Impact table with each lane resolved to files and validation, or `N/A`.
- `cook` must update the plan when implementation discovers harness impact and must run `harness/evals/run.ps1 -Layer harness` after harness changes.

The only committed eval tree is `harness/evals/`. Store runtime-orchestration eval cases there and do not create a root `evals/` tree.

## Harness Skill Operations

Dedicated lane skills keep harness work explicit:

- `harness-evals` for eval cases, fixtures, runner assertions, and evidence format.
- `harness-orchestrator` for runtime routing, handoffs, retries, approvals, and stop conditions.
- `harness-policies` for guardrails, permissions, approvals, protected paths, and policy evals.
- `harness-telemetry` for traces, logs, metrics, sanitized run evidence, and improvement-loop records.
- `harness-tools` for hosted tool, MCP, and local CLI adapter contracts.

When adding another harness lane, create the repo-local skill with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/New-HarnessSkill.ps1 -Lane <lane>
```

Then update `AGENTS.md`, this source memory, and harness eval coverage in the same change.

## Runtime Artifact Policy

The `harness/` directory stores machine-readable contracts, not placeholder README files.

Required artifacts:

- `harness/manifest.json`
- `harness/orchestrator/task-spec.schema.json`
- `harness/orchestrator/routing.json`
- `harness/policies/runtime-policy.json`
- `harness/telemetry/events.schema.json`
- `harness/tools/registry.json`

Validate them with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Get-HarnessStatus.ps1 -Json
```

The status command must fail if placeholder `README.md` files appear under the harness runtime contract directories.
