---
description: >-
  Slash /plan — orchestrator-workers. Read spec; parallel subtasks plan-domain-researcher,
  plan-application-researcher, plan-infrastructure-researcher, plan-web-researcher (if UI),
  graph-impact-analyst; write .opencode/plans/<spec-filename>.md. No product code. Then /build.
---
# /plan — Orchestrator-workers (spec → ephemeral plan)

You are the **orchestrator** (Tech Lead). Workers research in parallel; **you** synthesize the plan. **Planning only** — no product code, no GitHub updates, no spec body edits (optional frontmatter `plan_ready: true` only).

> **Layer 5 topology:** Workers are **readonly** only — parallel OK. **You alone** write `.opencode/plans/` (single writer). Never spawn parallel writers.

## Input

- Spec: `docs/specs/<YYYYMMDDHHmmss>-<name>.md`, or newest spec
- `--dry-run` — validate only, do not write plan

## Step 1: Orchestrator reads (sequential — prompt chaining)

Read in order before delegating:

1. Spec (ACs, scope, edge cases)
2. [`docs/constitution.md`](docs/constitution.md) · [`docs/ddd.md`](docs/ddd.md) · [`docs/technical.md`](docs/technical.md)
3. [`architecture.md`](.opencode/rules/architecture.md) · applicable scoped rules
4. `.opencode/notes/progress.md` if present

Split ACs into **research workstreams** (skip empty streams):

| Stream | Subagent | Focus |
|--------|----------|-------|
| Domain | `plan-domain-researcher` | Aggregates, `INV-*`, events |
| Application / CQRS | `plan-application-researcher` | Commands, queries, handlers, ports |
| Infrastructure / Api | `plan-infrastructure-researcher` | Persistence, HTTP, integration tests |
| Web | `plan-web-researcher` | Routes, Query, UI (if spec touches frontend) |
| Impact (parallel, recommended) | `graph-impact-analyst` | Blast radius via neo4j-graphrag MCP |

## Step 2: Parallel workers (subtasks — same turn, multiple agents)

Launch **one named subagent per stream in parallel** — do **not** use ad-hoc exploration. Example prompt:

```text
Spec: docs/specs/<file>.md
ACs: AC-01, AC-02
Return your structured research summary only (see your agent definition).
```

| # | Agent |
|---|-------|
| 1 | `@plan-domain-researcher` |
| 2 | `@plan-application-researcher` |
| 3 | `@plan-infrastructure-researcher` |
| 4 | `@plan-web-researcher` (if UI) |
| 5 | `@graph-impact-analyst` (recommended) |
| 6 | `@codebase-explorer` (optional — quick path:line scout if topic is narrow) |

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

**Path:** `.opencode/plans/<same-filename-as-spec>.md`

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

`/build .opencode/plans/<file>.md`

Update `progress.md` **Goal** + **Next** (plan path, branch).

## DO NOT

- Write product code · edit spec body · save under `docs/plans/` · `git add` the plan file
- Run `/build` in the same turn — hand off to user or `/build`
