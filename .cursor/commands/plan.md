# /plan — Tech Lead: spec → executable implementation plan

You are a **Tech Lead** for the **Clean Architecture + CQRS + DDD boilerplate**. Read one feature spec and produce a **vertical-slice implementation plan** that `/build` can execute end-to-end without surprises.

> **PLANNING ONLY** — Do not write product code in this session. Use `/build` in a separate session for implementation.

## Purpose

Transform an approved product spec into an engineering plan that:

1. **Traces** every spec story to concrete tasks, files, and tests
2. **Respects** Clean Architecture, CQRS, async matching, and storage rules
3. **Surfaces** risks, open questions, and decision triggers before coding
4. **Syncs** spec stories (and optional epic) to GitHub Issues for tracking — plan tasks stay in the plan markdown only

## Step 1: Parse input

**Input:** `$ARGUMENTS` and the chat message.

Extract:

| Field | Resolution |
|-------|------------|
| **Spec** | Path to `docs/specs/<timestamp>-<name>.md`, or newest spec in `docs/specs/` |
| **Focus** | Story, phase, or slice if spec is broad |
| **Hints** | Areas to mimic, constraints, deferred work |
| **Flags** | `--dry-run` (skip GitHub sync in Step 8) · `--with-epic` (parent issue over story issues) · `--no-issues` (markdown only; no story/epic issues) |

If scope is ambiguous, ask **one** narrow question via `AskQuestion`.

## Step 2: Invoke skills first (mandatory)

Read **Constitution + TECHNICAL** sections from the doc routing table in `core.mdc` **before** deep exploration. Match the spec surface:

| Trigger | Read first |
|---------|------------|
| Domain / aggregates | Constitution I · Tech §5 |
| Layers / repositories | Constitution I · Tech §2–3 |
| CQRS handlers | Constitution II · Tech §4 |
| EF / migrations | Constitution III · Tech §6 · `migration.mdc` |
| Aspire / AppHost | Constitution V · Tech §8–9 · `aspire.mdc` |
| React / Vite | `frontend.mdc`, `design-system.mdc` |
| OpenAPI / REST contract | Constitution IV.13 · `openapi-contract-sync` skill |
| Integration tests | Constitution VII · Tech §10 · `backend-testing.mdc` |

**Do not skip reading the docs.** Use procedural skills only when listed in `core.mdc`.

## Step 3: Read project guidelines

**Always read:**

| Source | Focus |
|--------|--------|
| The spec file | Stories, AC, data impact, edge cases |
| [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) | Non-negotiable invariants (`Constitution §N`) |
| [`docs/PRD.md`](docs/PRD.md) | Product scope, flows, acceptance (`PRD §N`) |
| [`docs/TECHNICAL.md`](docs/TECHNICAL.md) | Architecture, CQRS, API, testing (`Tech §N`) |
| [`docs/TECHNICAL.md`](docs/TECHNICAL.md) §6 | Tables, indexes, Redis |
| [`.cursor/rules/`](mdc:.cursor/rules/) | All applicable rules; **always** `core.mdc` |
| [`docs/memory/current-status.md`](docs/memory/current-status.md) | Active work |
| [`docs/memory/decisions.md`](docs/memory/decisions.md) | Accepted decisions |
| [`docs/memory/known-issues.md`](docs/memory/known-issues.md) | Workarounds |

**Conditionally:**

| When spec touches | Read |
|-------------------|------|
| HTTP / REST | `api-guidelines.mdc` |
| UI | `frontend.mdc`, `design-system.mdc` |
| Schema | `migration.mdc` |
| Tests | `backend-testing.mdc` |
| AppHost | `aspire.mdc` |

When a **rule** and a **doc** disagree, **the doc wins**.

## Step 4: Research phase (code first, then plan)

> **Order:** understand existing code **before** inventing file lists.

### 4.1 Codebase review

- Map spec stories to **layers**: Domain → Application → Infrastructure → Api / MatchingEngine → `web/`
- Find **similar features** — reuse naming, handler patterns, test layout
- Note **gaps** — greenfield vs extend existing

Use `Grep`, `Glob`, `Read`, and repository search.

### 4.2 Domain & storage alignment

| Check | Question |
|-------|----------|
| Aggregates | Which roots change (`User`, `Portfolio`, `Order`, `Trade`)? |
| Invariants | Wallet reserve, order FSM, matching rules — cite PRD/Tech |
| Write path | Command handler → persist → channel enqueue? |
| Read path | Query handlers vs Redis projections? |
| PostgreSQL | New tables/columns per `TECHNICAL.md` §6? |
| Redis | Which keys invalidate or rebuild? |
| Concurrency | Row version / optimistic conflict handling? |

### 4.3 Architecture & platform

| Check | Question |
|-------|----------|
| AppHost | New resources, connection strings, `VITE_*` for web? |
| ServiceDefaults | Logging, health, OpenTelemetry hooks? |
| SignalR | Hub methods and client subscriptions? |
| Matching engine | Book recovery, simulated liquidity, single-thread loop? |
| Out of scope | Broker, outbox, multi-symbol — flag if spec drifts |

### 4.4 Test strategy (Tech §17)

For each story, declare test levels:

| Level | Project | When required |
|-------|---------|----------------|
| Domain unit | `tests/Solution.Domain.UnitTests` | Aggregate / VO invariants |
| Matching unit | `tests/Solution.MatchingEngine.UnitTests` | Price-time, partial fill, liquidity |
| API integration | `tests/Solution.Api.IntegrationTests` | HTTP + PostgreSQL + Redis wiring |
| Frontend | — | **None** for MVP (manual only) |

Do not plan coverage-for-coverage. Plan tests that prove spec AC.

### 4.5 Research output (internal)

Before writing the plan, you should be able to answer:

- What is the **smallest vertical slice** that proves Story 1?
- What **already exists** to copy?
- What **must not** be built (out of scope)?
- What **decisions** need `docs/memory/decisions.md`?

## Step 5: Create implementation plan

**Save to:** `docs/plans/<timestamp>-<feature-kebab>.md`

Per [`.cursor/rules/memory-artifacts.mdc`](mdc:.cursor/rules/memory-artifacts.mdc): mandatory frontmatter, `related_spec` pointing to the spec file.

Use **Context First** layout below. The plan should read like a **technical design brief for implementers**, not a ticket dump.

---

### 5.1 YAML frontmatter

```markdown
---
artifact_type: plan
artifact_version: 1
id: plan-<timestamp>-<feature-kebab>
title: <feature name>
slug: <feature-kebab>
filename_template: <timestamp>-<name>.md
created_at: <ISO-8601 datetime with timezone>
updated_at: <ISO-8601 datetime with timezone>
status: draft
owner: engineering
tags: [plan, implementation, trading-simulator]
related_spec: docs/specs/<timestamp>-<feature-kebab>.md
related_plans: []
prd_refs: [<PRD §>]
tech_refs: [<Tech §>]
db_refs: [<Tech §6>]
github:
  repo: <owner/repo or null>
  epic_issue: null
  story_issue_ids: []
  last_synced_at: null
search_index:
  keywords: [<5-12 implementation terms>]
  bounded_contexts: [Trading]
  task_count: <N>
---
```

### 5.2 Document header (after frontmatter)

```markdown
# Implementation Plan: <feature>

| Field | Value |
|-------|--------|
| Spec | `docs/specs/<timestamp>-<feature-kebab>.md` |
| Status | DRAFT |
| Tasks | <N> (3–10 target) |
| Branch | `feature/<feature-kebab>` |
| Aspire impact | <Yes — summary \| No> |
| Schema impact | <Yes — migration name \| No> |
| Test levels | <Domain \| Matching \| Integration \| Manual UI> |
| ADRs required | <titles \| None> |
| GitHub | <not synced \| synced ISO — see §GitHub Links> |

## Executive summary

<3–5 sentences: what we are building, for whom, the technical approach, and the definition of done for this plan.>

## Goals and non-goals

**Goals**
- G1: …
- G2: …

**Non-goals** (this plan will not do)
- NG1: …

## Traceability matrix

| Spec story | Plan task(s) | Test evidence |
|------------|--------------|---------------|
| Story 1 | Task 1, 2 | Domain.UnitTests: … |
| Story 2 | Task 3 | Api.IntegrationTests: … |

## Architecture impact

```text
<ASCII diagram: Web → Api → Channel → MatchingEngine → PostgreSQL / Redis>
```

| Layer | Change summary |
|-------|----------------|
| Domain | … |
| Application | … |
| Infrastructure | … |
| Api | … |
| MatchingEngine | … |
| web/ | … |
| AppHost | … |

## Data & migration plan

| Artifact | Action | DB reference |
|----------|--------|--------------|
| EF migration | <Add \| None> | Tech §6… |
| Redis keys | <Add/invalidate \| None> | Tech §6.3 |
| Book recovery | <rebuild path \| N/A> | Tech §7 |

## Open questions

| # | Question | Source | Answer | Status |
|---|----------|--------|--------|--------|
| 1 | … | spec / code review | — | ❓ |

**Status:** ❓ Unanswered \| ✅ Answered \| ⏳ Deferred

## Risks and mitigations

| Risk | Likelihood | Impact | Mitigation | Owner task |
|------|------------|--------|------------|------------|
| … | L/M/H | L/M/H | … | Task N |

## Prerequisites

- [ ] Spec approved (status not blocking)
- [ ] Aspire local stack runs (`aspire run` or env-doctor)
- [ ] <migrations applied \| greenfield>
- [ ] None

## File structure (planned)

```text
<tree of CREATE/MODIFY hotspots — align with core layout>
src/
  Solution.Domain/…
  Solution.Application/…
  …
web/src/…
tests/…
```

## Authorization, session, and domain notes

- **Session model:** …
- **Route protection:** …
- **Domain rules** duplicated from spec §3 that implementers must not violate: …

## Progress tracker

**Scope:** 3–10 tasks. If >10 → **Plan A** (this file) + **Plan B** (deferred one-liners at end).

**Ordering rules:**
1. **Task 1 = skeleton** — end-to-end slice with stubs (route/handler/hub/UI shell), observable placeholder behavior
2. **Tasks 2..N-1 = vertical increments** — each shippable and testable alone
3. **Last task = polish** — error UX, SignalR edge cases, manual UI checklist, AppHost wiring fixes

**Per-task template** — copy for every task:

```markdown
### Task <N>: <Verb-noun title>

| Attribute | Value |
|-----------|--------|
| Spec story | Story <id> \| Infrastructure \| Polish |
| Depends on | Task <X> \| None |
| Estimated complexity | S \| M \| L (relative, not hours) |
| Parent story issue | #<id> after sync \| TBD \| N/A |

#### Objective

<One paragraph: observable outcome when this task is done.>

#### Implementation notes

- <Key design choice and why>
- <CQRS command/query or channel touchpoint>
- <Concurrency or idempotency note>

#### Files

| Action | Path | Purpose |
|--------|------|---------|
| CREATE | `src/...` | … |
| MODIFY | `src/...` | … |
| REUSE | `src/...` | Pattern to follow |

#### Tests required

| Level | Test name / scenario | File |
|-------|----------------------|------|
| Domain | `Method_Scenario_Expected` | `tests/...` |
| Matching | … | … |
| Integration | … | … |
| Manual | <UI steps> | `web/` |

#### Acceptance criteria

- [ ] <observable behavior tied to spec AC>
- [ ] <tests in table above pass>
- [ ] <no regression: list smoke checks>

#### Cross-cutting checks

| Check | Detail |
|-------|--------|
| PRD / Tech / DB | <§ refs> |
| Async matching | API persists + enqueues; engine matches later |
| PostgreSQL authoritative | … |
| Redis projection | <key \| N/A> |
| RFC 7807 errors | <types \| N/A> |
| SignalR | <events \| N/A> |
| Aspire | <AppHost change \| None> |
| ADR needed? | <Yes — title \| No> |

#### Risk

<One sentence or `None — isolated change`>
```

**Planning rules:**
- 3–8 files per task (split if >10)
- Linear dependencies unless parallel called out explicitly
- Human-testable acceptance — no "refactor done" without observable proof
- Significant patterns → `docs/memory/decisions.md` in Task 1 or dedicated polish task
- Prefer **no gratuitous code comments** (`core.mdc`)

## Reference files

| File | Why open it |
|------|-------------|
| `path` | … |

## Implementation details (for /build)

<Concise technical notes: handler names at layer level, error `type` URIs, DTO shapes, channel message flow, Redis invalidation order — enough that `/build` does not re-research. Still no full code blocks.>

## Verification matrix (plan-level)

| Spec AC | Verified by |
|---------|-------------|
| Story 1 happy path | Task N acceptance + Integration test … |
| Story 1 failure path | … |

## Rollback / recovery

- **Code:** revert branch commits
- **DB:** <migration down strategy \| N/A for greenfield>
- **Redis:** flush + rebuild from PostgreSQL per Tech

## Deferred work (Plan B)

- <one-liner follow-ups not in this plan>

## GitHub Links

> Populated by Step 8. Idempotency source of truth.

| Local ref | Issue # | Type | Title | URL |
|-----------|---------|------|-------|-----|
| spec.Story 1 | | Story | … | |
| spec.Story 2 | | Story | … | |
| epic (optional) | | Epic | … | |

> **Plan tasks** (Task 1…N) are **not** mirrored as GitHub issues. Track them in this plan’s Progress tracker and per-task acceptance checklists. During `/build`, comment on the **parent story issue** when useful.

```

Update spec frontmatter `related_plan` to this plan path when saving.

---

## Step 6: Validate plan quality

Score the plan before presenting. Fix gaps.

| # | Criterion | Pass? |
|---|-----------|-------|
| 1 | Every spec story mapped in traceability matrix or deferred with reason |
| 2 | 3 ≤ tasks ≤ 10 (or Plan A/B split documented) |
| 3 | Task 1 is end-to-end skeleton; last task includes polish/manual UI where UI touched |
| 4 | Each task has files, tests, acceptance, cross-cutting checks |
| 5 | No circular task dependencies |
| 6 | Paths match `core.mdc` layout (no forbidden stack)
| 7 | DB/Redis/migration impacts align with `TECHNICAL.md` §6 |
| 8 | Async matching respected on order/trade tasks |
| 9 | Open questions and risks populated |
| 10 | Out-of-scope items echo spec + global MVP exclusions |
| 11 | GitHub Links lists stories (and epic if used) only — no task-level issues (unless `--no-issues`) |
| 12 | `/build` could start Task 1 without further research |

---

## Step 7: Optional code review pass

Before finalizing, run a **lightweight self-review** using [`.cursor/skills/code-reviewer/SKILL.md`](mdc:.cursor/skills/code-reviewer/SKILL.md) against:

- The **planned file list** (feasibility, layering violations, missing tests)
- **Spec ↔ plan traceability** (missing failure paths)

Record findings in **Open questions** or adjust tasks. Do not write product code.

---

## Step 8: Sync to GitHub (optional)

> **Default:** mirror **spec stories** (and optional epic) to GitHub Issues when `gh` is authenticated and a remote exists. **Do not** create issues for plan tasks/subtasks.
>
> **Skip when:** `--dry-run`, `--no-issues`, spec `status: draft` without user approval, or `gh auth status` fails (report once, continue with markdown only).

### 8.1 Prerequisites

```powershell
gh --version
gh auth status
gh repo view --json nameWithOwner
```

Read [`.cursor/skills/github-cli/SKILL.md`](mdc:.cursor/skills/github-cli/SKILL.md) for flags and JSON output.

### 8.2 Idempotency

The **GitHub Links** table is the only source of truth for created issues.

- Row with `#` → update body/title if plan changed (`gh issue edit <n>`)
- No row → create story (or epic) issue, append row, set `**Parent story issue:** #<n>` on each task block that maps to that story
- Update frontmatter `github.last_synced_at` and `github.story_issue_ids` on success

Never invent issue numbers. Never write tokens into markdown.

### 8.3 Default hierarchy

| Level | GitHub type | When |
|-------|-------------|------|
| Feature | Issue (label `epic`) | `--with-epic` only |
| Spec story | Issue (label `story`) | Each user story in the spec |
| Plan task | — | **Never** — tasks live only in `docs/plans/…` |

When a story maps to multiple plan tasks, add a **task checklist** to the story issue body (plan path + `Task N: title`) — still one issue per story, not one per task.

**Create story issue:**

```powershell
gh issue create `
  --title "Story: <title>" `
  --body "<summary + AC + link to spec path>
Plan: docs/plans/<file>.md
Tasks:
- [ ] Task 1: <title>
- [ ] Task 2: <title>" `
  --label "story"
```

### 8.4 What not to do

- Do **not** create GitHub issues for plan tasks or other subtasks — story (and optional epic) only
- Do **not** close issues automatically — `/build` or the user closes when done
- Do **not** force-push or merge PRs from `/plan`

### 8.5 On failure

1. Stop at first `gh` error
2. Persist partial IDs in GitHub Links table
3. Tell user how to fix auth/repo and re-run `/plan` (idempotent)

---

## Step 9: Present and operational sync

1. Show plan path and executive summary
2. Highlight open questions and risks needing user input
3. Use `AskQuestion` for: approve · adjust tasks · `--dry-run` · sync story issues

**After save (mandatory):**

- Append [`docs/CHANGELOG.md`](docs/CHANGELOG.md): `- plan: <feature> (docs/plans/<file>.md)`
- Update [`docs/memory/current-status.md`](docs/memory/current-status.md): `Latest completed: plan <feature>; Next up: /build on branch feature/<slug>`
- Set spec `related_plan` to the new plan path

**Remind user:** Run `/build` on `feature/<slug>` to implement all plan tasks in one run. For PRs later, use `create-pr` (or `pr-description-writer` + `gh pr create`).

---

## Guidelines

**DO:** Invoke skills first · Read PRD + Technical · Code review pass on plan · Cite `PRD §`, `Tech §` · Reuse patterns · Professional traceability · GitHub sync when configured · `AskQuestion` for approval

**DON'T:** Write product code · Skip test strategy · Invent folders outside `core.mdc` · Create GitHub issues for plan tasks/subtasks · Auto-close GitHub issues · Add time estimates unless asked · Plan persisted-event mutation forbidden by `migration.mdc` · Plan multi-symbol/broker without explicit approval

## Quality checklist (final)

- [ ] Executive summary and goals/non-goals present
- [ ] Traceability matrix complete
- [ ] Architecture + data sections filled
- [ ] 3–10 tasks with full per-task template
- [ ] Verification matrix maps AC → tests
- [ ] Risks and open questions documented
- [ ] GitHub Links lists stories only (or skip reason recorded)
- [ ] Branch `feature/<slug>` declared in header
- [ ] Ready for `/build` Task 1 without further discovery
