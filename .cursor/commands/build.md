# /build — Implement a plan end-to-end

Execute a plan **continuously**: work through every unchecked task in order until the plan is complete, you are blocked, or the user stops you. **Do not stop after one task** unless blocked or explicitly asked to pause.

**PARALLEL-FIRST:** Use parallel reads and independent lookups while scoping work. **Implementation** for a given task stays sequential; parallelize only preparation (unrelated files, rules, pattern search).

**CLARIFICATION:** When you need to ask questions, use `AskQuestion` instead of dumping questions as plain text.

> **PLAN SYNC RULE (MANDATORY):** Any **deviation** from the plan (different approach, new sub-tasks, scope changes) **MUST** be written back to the plan file **immediately**. Task checkmarks (`[ ]` → `[x]`) update after each task completes; the plan must always reflect the actual approach.

## Step 1: Invoke docs and procedural skills (conditional)

**Architecture and patterns:** read Constitution + TECHNICAL (see `core.mdc` doc routing). **Do not load removed overlay skills.**

**Skills depend on who implements:**

| Scenario | Action |
|----------|--------|
| **Main agent implements** (default) | Read applicable `.cursor/skills/<name>/SKILL.md` before heavy implementation (once per plan run, re-read when surface changes) |
| **Delegating to subagents** | Put skill names in the subagent prompt — do not double-invoke here |

### Skill routing (Clean Architecture boilerplate)

### Skill routing (procedural only — architecture is in docs)

| Trigger | Read / skill |
|---------|----------------|
| Architecture, domain, CQRS, EF, tests | Constitution + TECHNICAL (`core.mdc` doc routing) |
| AppHost / local run | `aspire.mdc`, Tech §8–9 |
| OpenAPI export / codegen | `openapi-contract-sync` |
| Live DB via MCP | `postgres-mcp` |
| Frontend data fetching | `tanstack-query` |
| shadcn / Tailwind | `shadcn`, `tailwind-patterns` |
| Local env broken | `env-doctor` |
| User asks to **commit** | `git-commit-writer` |
| User asks for **PR** or full ship workflow | `create-pr` (or `pr-description-writer` + `github-cli`) |
| User asks for **code review** | `code-reviewer` |
| GitHub issue URL or `#<n>` in input | `github-cli` |

## Step 2: Read guidelines

**Always:**

- [`.cursor/rules/core.mdc`](mdc:.cursor/rules/core.mdc)
- [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md), [`docs/PRD.md`](docs/PRD.md), [`docs/TECHNICAL.md`](docs/TECHNICAL.md) — skim sections the plan touches

**Conditionally** (match plan surface):

| Surface | Rules |
|---------|--------|
| .NET services | `backend.mdc` |
| Tests | `backend-testing.mdc` |
| React UI | `frontend.mdc`, `design-system.mdc` |
| HTTP / SignalR | `api-guidelines.mdc` |
| EF / schema | `migration.mdc` |
| AppHost | `aspire.mdc` |

## Step 3: Parse input

**Input:** `$ARGUMENTS` and the chat message.

| Input type | Detection | Action |
|------------|-----------|--------|
| Plan path | `docs/plans/<timestamp>-<name>.md` | Use that file |
| Start task | `task 3`, `Task 2`, phase name | Begin at that task; **continue through remaining unchecked tasks** |
| GitHub issue | `#123` or `github.com/.../issues/123` | `gh issue view` for context (`github-cli`) |

If unclear, use `AskQuestion` for plan path or start task.

## Step 4: Build context

- **Plan:** `docs/plans/<timestamp>-<name>.md`
- **Spec:** path from plan frontmatter `related_spec` → `docs/specs/<timestamp>-<name>.md`
- **Memory:** `docs/memory/current-status.md`, `decisions.md`, `known-issues.md`
- **Task queue:** all unchecked acceptance items in plan order; if a start task was given, skip earlier tasks

**Branch (mandatory per plan):**

- Use `> Branch: feature/<plan-slug>` from plan header when present.
- Else derive kebab slug from plan filename → `feature/<plan-slug>`.
- Create from `main` if missing; **never** implement plan work on `main` or unrelated branches.

**Artifact validation:**

- Filenames: `<timestamp>-<name>.md`, timestamp `YYYYMMDD-HHmmss`
- YAML frontmatter per `memory-artifacts.mdc`
- Fix metadata before coding if missing

**Exploration:** `Read`, `Grep`, `Glob` — reuse existing patterns; do not invent new stack choices.

If **all tasks are already checked**, report completion and stop — do not invent work.

## Step 5: Analyze parallelism

| Condition | Approach |
|-----------|----------|
| Independent reads (MODIFY/REUSE, rules) | Parallel |
| Writes / implementation per task | Sequential within that task |
| Optional `explore` subagent | Parallel with reads if prompts are independent |

**Sequential signals:** task says "after Task X", "depends on", or blocked on migration — respect order across the queue.

## Step 6: Execute (continuous loop)

Use `TodoWrite` for the **full plan queue** (all remaining tasks), not a single task.

### 6.0 Plan kickoff (once per `/build` run)

Before the first edit:

1. Output a short **plan pre-flight**: plan path, branch, task count remaining, ordered task titles, shared risks, validation commands you will run.
2. **Stop for explicit user confirmation** (`ok` / `go`) before any edits or commits unless the user waived confirmation this session.
3. After confirmation, **enter the task loop** — do not ask again between tasks unless blocked or scope must change.

### 6.1 Task loop (repeat until done or blocked)

For **each** unchecked task, in plan order:

1. **Prepare** — read every `CREATE` / `MODIFY` / `REUSE` file for this task; search for patterns to reuse; verify package versions if adding dependencies.
2. **Brief task note** (1–2 sentences in chat): task id, files touched, approach — then **implement immediately** (no per-task confirmation gate).
3. **Implement** — minimal scope for this task only; respect layer order when the task implies it (domain → application → infrastructure → API/engine → web).
4. **Validate** — run tests/commands required by this task (see Step 8); fix failures before moving on.
5. **Update plan** — mark task `[x]`; record deviations immediately (Step 7).
6. **Continue** to the next unchecked task without pausing for user approval.

**Invariants (every task):** Clean Architecture, pure Domain, async matching, PostgreSQL authoritative, Redis projections rebuildable, ServiceDefaults on hosts.

**Other rules:**

- **AppHost:** `src/Solution.AppHost` (or current AppHost path) — no hand-authored `docker-compose` for orchestration
- **Migrations:** `migration.mdc` — never edit applied EF migrations
- **API:** RFC 7807, contracts DTOs, routes per `api-guidelines.mdc`
- **UI:** loading, empty, error, reconnecting states per `design-system.mdc`
- **Decisions:** significant architecture choices → `docs/memory/decisions.md`
- **Plan sync:** deviations → update plan immediately (Step 7)

If a task is **blocked** (missing prerequisite, ambiguous AC, env failure after `env-doctor`): mark `[SKIP]` + reason in plan Notes, report to user, and **stop the loop** unless the user says to skip and continue.

If scope **explodes** beyond the plan, use `AskQuestion` once to narrow or split — then update the plan and continue.

### 6.2 Main agent responsibilities

- Orchestration, plan kickoff confirmation, continuous task execution
- Build / test / lint after each task (or batched when the plan allows)
- Plan updates (especially deviations)
- Final validation when all tasks complete

### 6.3 Optional exploration subagent

```
Explore [area] for patterns related to [task from <plan file>].
Return: file paths, patterns, snippets. Do not modify files.
```

## Step 7: Update plan (critical)

The plan file is the source of truth for execution.

| Category | When | Examples |
|----------|------|----------|
| **Deviations** | Immediately | Different approach, extra files, split task |
| **Completions** | After each task | `[ ]` → `[x]` |

- Blocked: `[SKIP]` + reason in plan Notes
- **Memory sync:** update `current-status.md` when the **plan run finishes** (or stops blocked); `CHANGELOG.md` if material; `known-issues.md` if new bug found
- **GitHub:** if the plan’s **Parent story issue** has `#<n>`, optionally comment progress when the run completes or stops — do not create task-level issues; do not close unless user asks
- **Do not commit** unless the user explicitly asks (then use `git-commit-writer`)

## Step 8: Validate

Run from repo root. Match each task’s **Tests required** line as you complete it; run a **final full pass** when all tasks are done.

**Backend** (adjust solution path when present):

```powershell
dotnet build Solution.sln -c Release --nologo
dotnet test  Solution.sln -c Release --nologo
```

If no `.sln` yet, build/test the projects listed in the task.

**Per `backend-testing.mdc` when applicable:**

```powershell
dotnet test tests/Solution.Domain.UnitTests -c Release --nologo
dotnet test tests/Solution.MatchingEngine.UnitTests -c Release --nologo
dotnet test tests/Solution.Api.IntegrationTests -c Release --nologo
```

**Format** (when solution exists):

```powershell
dotnet format Solution.sln --verify-no-changes
```

**Aspire** (AppHost / topology changed):

```powershell
aspire run --project src/Solution.AppHost
```

Use **aspire-mcp** or `aspire` skill if dashboard/resources fail.

**Frontend** (`web/` when plan touches UI):

```powershell
yarn --cwd web lint
yarn --cwd web build
```

**EF** (schema changed — paths per task / `migration.mdc`):

```powershell
dotnet ef migrations add <Name> -p src/Solution.Infrastructure -s src/Solution.Api
dotnet ef database update      -p src/Solution.Infrastructure -s src/Solution.Api
```

Never modify existing migration files that were already applied.

## Step 9: Summary

Report when the loop ends (complete, blocked, or user-stopped):

- Tasks completed (ids and titles) and any `[SKIP]` with reasons
- Plan checkbox status (all done vs remaining)
- Tests run and results (per task + final pass)
- Deviations recorded in plan
- Suggested next action (open PR via `create-pr`, commit if user wants, fix blocker, etc.)

---

## Examples

| Command | Action |
|---------|--------|
| `/build` | All unchecked tasks in latest / linked plan |
| `/build docs/plans/20260523-140000-place-order.md` | All remaining tasks in that plan |
| `/build docs/plans/20260523-140000-place-order.md task 2` | Task 2 onward through end of plan |
| `/build` (all done) | Report completion; do not invent work |

### Execution pattern

```
1. Skills + rules + plan + spec + DB sections
2. Plan kickoff pre-flight → one user confirmation
3. FOR EACH unchecked task:
   a. Parallel reads (MODIFY / REUSE / patterns)
   b. Brief task note → implement → validate → checkbox + deviations
4. Final validation (full solution / web if needed)
5. Finalize memory files + summary
```

### Dependency rule

**One task in flight at a time**, but **many tasks per `/build` session**. Respect plan task order and cross-task dependencies; do not skip ahead when a task is blocked on a prior one.

---

## Finalize

- Plan: all completed checkboxes for finished tasks; deviation notes current
- `docs/memory/current-status.md` updated for the plan run outcome
- Git: commit only on explicit request (`git-commit-writer`)
- PR: only on explicit request (`create-pr` or `pr-description-writer` + `gh pr create`)
