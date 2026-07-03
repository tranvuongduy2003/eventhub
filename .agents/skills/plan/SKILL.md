---
name: plan
description: Read a spec, research the codebase in parallel, and write an implementation plan. Use when a product spec needs engineering tasks, files, and validation steps.
---

# Plan

You are the orchestrator (Tech Lead). Workers research in parallel; you synthesize the plan. Planning only — no product code, no GitHub updates, no spec body edits (optional frontmatter `plan_ready: true` only).

> Layer 5 topology: Workers are readonly only — parallel OK. You alone write `.codex/plans/` (single writer). Never spawn parallel writers.

## Input

- Spec: `docs/_memory/specs/<YYYYMMDDHHmmss>-<name>.md`, or newest spec
- `--dry-run` -> validate only, do not write plan

## Step 1: Orchestrator reads (sequential — prompt chaining)

Read in order before delegating:

1. Spec (ACs, scope, edge cases)
2. `docs/CONSTITUTION.md` · `docs/_memory/source/domain-model-specification.md` · `docs/_memory/source/technical-design.md`
3. `docs/_memory/source/harness-architecture.md` and `docs/_memory/source/harness-operational-policies.md`
4. the current task notes if present

Split ACs into research workstreams (skip empty streams):

| Stream | Subagent | Focus |
|--------|----------|-------|
| Domain | `plan-domain-researcher` | Aggregates, `INV-*`, events |
| Application / CQRS | `plan-application-researcher` | Commands, queries, handlers, ports |
| Infrastructure / Api | `plan-infrastructure-researcher` | Persistence, HTTP, integration tests |
| Web | `plan-web-researcher` | Routes, Query, UI (if spec touches frontend) |
| Impact (parallel, recommended) | `graph-impact-analyst` | Blast radius via neo4j-graphrag MCP |

## Step 2: Parallel workers (Agent tool — same message, multiple subagents)

Launch one named subagent per stream in parallel — do not use generic `Explore`.

| Call | Agent |
|------|-------|
| 1 | `@agent-plan-domain-researcher` |
| 2 | `@agent-plan-application-researcher` |
| 3 | `@agent-plan-infrastructure-researcher` |
| 4 | `@agent-plan-web-researcher` (if UI) |
| 5 | `@agent-graph-impact-analyst` (recommended) |
| 6 | `@agent-codebase-explorer` (optional — quick path:line scout if topic is narrow) |

Workers: readonly only · parallel OK · no product code · no plan file.

## Step 3: Routing (orchestrator merges)

| Finding | Route into plan as |
|---------|-------------------|
| AC -> files mapping | Task list with concrete paths |
| Spec Harness Impact -> evals, orchestrator, policies, telemetry, tools | Dedicated Harness Impact section + task(s) when changed |
| Cross-cutting concern | Dedicated task or Notes |
| Unknown / conflict | Blockers section + ask user if blocking |
| Out of scope | Omit (see `docs/_memory/source/product-requirements.md` §6.2) |

De-duplicate overlapping worker results. Prefer Constitution -> spec -> domain model source memory on conflicts.

## Harness Impact Triage

Every plan must include a `## Harness Impact` section before the task list. Use the spec's Harness Impact section as the starting point, then verify against the intended files.

Track each lane explicitly:

| Lane | Plan entry |
|------|------------|
| `harness/evals/` | New/changed deterministic or manual cases, fixtures, runner assertions, or `N/A` |
| `harness/orchestrator/` | Runtime routing, handoff, retry, approval, or stop-condition changes, or `N/A` |
| `.codex/policies/`, `harness/policies/` | Guardrail, permission, approval, protected path, or verification policy changes, or `N/A` |
| `harness/telemetry/` | Trace/log/metric/evidence changes, or `N/A` |
| `harness/tools/` | Tool adapter, MCP, CLI, hosted-tool contract changes, or `N/A` |
| Workflow surfaces | `.agents/skills/`, `.codex/hooks/`, `scripts/agent/`, `.graph/`, AGENTS.md changes, or `N/A` |

If any lane is not `N/A`, add concrete tasks and validation commands for that harness work. Do not hide harness changes inside product tasks.

## Step 4: Write plan file

**Path:** `.codex/plans/<same-filename-as-spec>.md`

```yaml
---
related_spec: docs/_memory/specs/<timestamp>-<feature-kebab>.md
branch: feature/<slug>
created_at: <ISO-8601>
---
```

```markdown
# Plan: <title>

## Harness Impact

| Lane | Impact | Files | Validation |
|------|--------|-------|------------|
| evals | N/A or ... | `harness/evals/...` | `powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness` |
| orchestrator | N/A or ... | `harness/orchestrator/...` | ... |
| policies | N/A or ... | `.codex/policies/...` / `harness/policies/...` | ... |
| telemetry | N/A or ... | `harness/telemetry/...` | ... |
| tools | N/A or ... | `harness/tools/...` | ... |
| workflow | N/A or ... | `.agents/skills/...` / `.codex/hooks/...` / `scripts/agent/...` / `.graph/...` / `AGENTS.md` | ... |

**3–8 tasks.** Task 1 = vertical skeleton; last = polish/tests.

| AC | Tasks |
|----|-------|
| AC-01 | 1, 2 |

### Task 1: <title>
- **AC:** AC-01
- **Files:** CREATE `path` · MODIFY `path`
- **Notes:** one line (include graph/worker findings)
- [ ] Done
```

## Step 5: Validate

Every AC mapped · Harness Impact table completed · 3–8 tasks · concrete paths · `build` can start without more research

## Present

Plan path, task summary, branch, worker highlights. Remind:

`build .codex/plans/<file>.md`

Update the current task notes Goal + Next (plan path, branch).

## DO NOT

- Write product code · edit spec body · save under `docs/plans/` · `git add` the plan file
- Run `build` in the same turn — hand off to user or `build`
