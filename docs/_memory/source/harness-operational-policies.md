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
powershell -NoProfile -ExecutionPolicy Bypass -File evals/run.ps1 -Layer harness
```

For `.graph/index.json` or `scripts/affected-tests.mjs` changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File evals/run.ps1 -Layer graph
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
- `evals/results/`

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
