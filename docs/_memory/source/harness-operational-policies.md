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
The canonical runtime manifest lives in `harness/manifest.json`.

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

For `harness/graph/index.json` or `scripts/affected-tests.ps1` changes:

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

## Cook Unified Contract

Harness impact and long-term memory sync are part of normal delivery, not afterthoughts:

- `cook-unified` is the only feature-delivery workflow surface. Its internal phases are `spec`, `plan`, implementation checkpoints, `verify`, memory sync, and handoff.
- `cook-unified` has an audit/dry-run mode for prompts containing `--dry-run`, `audit`, or `trace-only`. Dry-run may read source context and emit a trace of intended artifacts, likely validation, and adjacent-feature risks, but it must not create durable specs, plans, progress notes, code changes, or memory updates unless the user separately asks for a durable audit report.
- The spec phase must include a Harness Impact section and state `N/A` only when evals, orchestrator, policies, telemetry, tools, hooks, skills, scripts, graph, and AGENTS.md are unaffected.
- The spec phase must update the relevant memory indexes so the new durable spec is discoverable before planning starts. Feature specs normally update `docs/_memory/mocs/feature-roadmap.md`; other specs may require source maps, MOCs, glossaries, retrieval guides, README/index files, or harness memory.
- Feature-id specs and plans must include an Adjacent Feature Boundary that names neighboring features, in-scope behavior, and out-of-scope behavior.
- The plan phase must include a Harness Impact table with each lane resolved to files and validation, or `N/A`.
- The plan phase must include a Memory Sync inventory owned by `memory-sync` covering related spec status, source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data, related issues or handoff evidence when applicable, and `scripts/agent/Test-DocsMemory.ps1` validation.
- The plan phase must include a Surface Completeness Review covering Backend/API, Frontend/web, OpenAPI/codegen, E2E/Playwright, DevOps/Aspire, Docs/memory, and Harness/workflow. Any `N/A` must carry a product or technical rationale. User-visible workflow changes must include frontend work or record why existing UI already satisfies the acceptance criteria; critical browser workflows should include Playwright coverage unless the plan records why narrower checks are sufficient.
- The plan phase must include a Done Criteria Ledger and must validate the plan, progress note, and TaskSpec sidecar with `scripts/agent/Test-CookPlan.ps1` before implementation starts.
- The implementation phase must update the plan when it discovers harness impact, product-surface impact, or memory drift and must run `harness/evals/run.ps1 -Layer harness` after harness changes.
- Cook must not delete the plan or declare done until the related spec is marked `implemented`, every affected long-term memory and harness contract surface is current or explicitly `N/A`, the Done Criteria Ledger is complete or explicitly `N/A`, docs-memory validation passes, and changed-code verification passes.

The only committed eval tree is `harness/evals/`. Store runtime-orchestration eval cases there and do not create a root `evals/` tree.

## Harness Skill Operations

Dedicated lane skills keep harness work explicit:

- `harness-evals` for eval cases, fixtures, runner assertions, and evidence format.
- `harness-orchestrator` for runtime routing, handoffs, retries, approvals, and stop conditions.
- `harness-policies` for guardrails, permissions, approvals, protected paths, and policy evals.
- `harness-telemetry` for traces, logs, metrics, sanitized run evidence, and improvement-loop records.
- `harness-tools` for hosted tool, MCP, and local CLI adapter contracts.
- `memory-sync` for completion audits across source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data, and handoff evidence. It is a workflow skill, not a runtime lane.

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
