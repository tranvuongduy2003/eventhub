---
description: "Read spec; parallel plan-domain-researcher, plan-application-researcher, plan-infrastructure-researcher, plan-web-researcher, graph-impact-analyst; write ephemeral .claude/plans/<spec-filename>.md. No product code. Then /build."
argument-hint: "[spec-file-path]"
disable-model-invocation: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Grep
  - Glob
  - Bash
  - Agent
---
# /plan — Orchestrator-workers (spec → ephemeral plan)

You are the **orchestrator** (Tech Lead). Workers research in parallel; **you** synthesize the plan. **Planning only** — no product code, no GitHub updates, no spec body edits (optional frontmatter `plan_ready: true` only).

> **Layer 5 topology:** Workers are **readonly** only — parallel OK. **You alone** write `.claude/plans/` (single writer). Never spawn parallel writers.

## Input

- Spec: `docs/specs/<YYYYMMDDHHmmss>-<name>.md`, or newest spec
- `--dry-run` — validate only, do not write plan

## Step 1: Orchestrator reads (sequential — prompt chaining)

Read in order before delegating:

1. Spec (ACs, scope, edge cases)
2. [`docs/constitution.md`](docs/constitution.md) · [`docs/ddd.md`](docs/ddd.md) · [`docs/technical.md`](docs/technical.md)
3. `architecture.md` · applicable scoped rules
4. `.claude/notes/progress.md` if present

Split ACs into **research workstreams** (skip empty streams):

| Stream | Subagent | Focus |
|--------|----------|-------|
| Domain | `plan-domain-researcher` | Aggregates, `INV-*`, events |
| Application / CQRS | `plan-application-researcher` | Commands, queries, handlers, ports |
| Infrastructure / Api | `plan-infrastructure-researcher` | Persistence, HTTP, integration tests |
| Web | `plan-web-researcher` | Routes, Query, UI (if spec touches frontend) |
| Impact (parallel, recommended) | `graph-impact-analyst` | Blast radius via neo4j-graphrag MCP |

## Step 2: Parallel workers (Agent tool — same message, multiple subagents)

Launch **one named subagent per stream in parallel** — do **not** use generic `Explore`. Example Agent call:

```text
@agent-plan-domain-researcher
Spec: docs/specs/<file>.md
ACs: AC-01, AC-02
Return your structured research summary only (see your agent definition).
```

| Call | Agent |
|------|-------|
| 1 | `@agent-plan-domain-researcher` |
| 2 | `@agent-plan-application-researcher` |
| 3 | `@agent-plan-infrastructure-researcher` |
| 4 | `@agent-plan-web-researcher` (if UI) |
| 5 | `@agent-graph-impact-analyst` (recommended) |
| 6 | `@agent-codebase-explorer` (optional — quick path:line scout if topic is narrow) |

Workers: **readonly** only · parallel OK · no product code · no plan file.

## Step 3: Routing (orchestrator merges)

| Finding | Route into plan as |
|---------|-------------------|
| AC → files mapping | Task list with concrete paths |
| Cross-cutting concern | Dedicated task or Notes |
| Unknown / conflict | **Blockers** section + ask user if blocking |
| Out of scope | Omit (see `prd.md` §6.2) |

De-duplicate overlapping worker results. Prefer **Constitution → spec → ddd** on conflicts.

## Step 4: Write plan file

**Path:** `.claude/plans/<same-filename-as-spec>.md`

```yaml
---
related_spec: docs/specs/<timestamp>-<feature-kebab>.md
branch: feature/<slug>
created_at: <ISO-8601>
---
```

```markdown
# Plan: <title>

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

Every AC mapped · 3–8 tasks · concrete paths · `/build` can start without more research

## Present

Plan path, task summary, branch, worker highlights. Remind:

`/build .claude/plans/<file>.md`

Update `progress.md` **Goal** + **Next** (plan path, branch).

## DO NOT

- Write product code · edit spec body · save under `docs/plans/` · `git add` the plan file
- Run `/build` in the same turn — hand off to user or `/build`
