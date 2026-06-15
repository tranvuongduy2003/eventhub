---
description: >-
  Slash /build — evaluator-optimizer. Execute .cursor/plans/ tasks: graph-impact-analyst per task,
  build-test-writer for bugs; parent alone writes production code; parallel readonly scouts
  (codebase-explorer, graph-impact-analyst, test-impact-analyzer); evaluator loop until green.
  All five agent layers — see agent-stack.mdc walkthrough (e.g. add Phone to User).
---
# /build — Evaluator-optimizer (implement plan until checks green)

Execute the ephemeral plan using the **evaluator-optimizer** loop: generate → verify with real tests/types → refine until objective checks pass. Harness `stop-gate` enforces this — self-declared "done" does not count.

> **PLAN SYNC:** Deviations → update plan immediately. Mark `[x]` after each task passes verification.

> **EPHEMERAL PLAN:** `.cursor/plans/` only. **Never commit.** **Delete** when all tasks complete.

## Pattern: evaluator-optimizer

| Phase | Action |
|-------|--------|
| Generate | Minimal code for current task |
| Evaluate | affected tests + type-check (commands below) |
| Optimize | Fix only what failed; repeat until green |

> **Layer 5 topology:** **You alone** write production code. Subagents are readonly scouts except `build-test-writer` (tests only, **sequential** — never parallel with other writers).

## Step 0: Read context

Constitution · `ddd.md` · `technical.md` · **`architecture.mdc`** · spec from plan `related_spec` · `.cursor/notes/progress.md`.

## Step 1: Parse input

| Input | Action |
|-------|--------|
| `.cursor/plans/<file>.md` | Use that plan |
| `docs/specs/<file>.md` | Use paired `.cursor/plans/<same-filename>.md` |
| Newest in `.cursor/plans/` | If no path |
| `task N` | Start at task N |
| `#123` | Context only |

Branch: `feature/<slug>` from plan frontmatter; create from `main` if missing.

If all tasks checked → delete plan → report done.

## Step 2: Context retrieval (parallel readonly — then you write)

Before each task, launch **parallel** readonly subagents in one message (summaries only — keep main window clean):

| subagent_type | When |
|---------------|------|
| `graph-impact-analyst` | Blast radius, callers (neo4j-graphrag MCP) |
| `codebase-explorer` | Locate files/symbols for the task topic |
| `test-impact-analyzer` | If prior commits/diff exist — tests to run + gaps |

Example:

```text
Task(subagent_type="graph-impact-analyst", prompt="F-… / files: … / spec: …")
Task(subagent_type="codebase-explorer", prompt="Find handlers and tests for …")
```

Merge summaries into `.cursor/notes/progress.md` **Decisions**. **Do not** let subagents edit code.

**Fallback** (MCP down): `SemanticSearch` + docs — note `degraded: no graph`.

## Step 3: Mandatory task loop (evaluator-optimizer)

For each unchecked task:

### 3a. If bug fix → red test first (sequential writer)

Delegate to **`build-test-writer`** alone — **not** in parallel with other writers:

```text
Task(subagent_type="build-test-writer", prompt="Bug: … / repro: … / layer: Domain|Integration")
```

Confirm red once. **Then you** implement the minimal production fix (single writer).

### 3b. Implement — minimal diff

Domain → Application → Infrastructure → Api → web. Match existing patterns; no drive-by refactors.

### 3c. Evaluate — affected checks (objective only)

Run commands yourself or delegate **`test-impact-analyzer`** (readonly) for scope — **you** run dotnet/yarn and interpret exit codes.

For each file you changed in this iteration:

```powershell
node scripts/affected-tests.mjs <path>
```

Run the returned steps (EventHub stack — not npm):

| Step kind | Command |
|-----------|---------|
| `dotnet-test` | `dotnet test <project> [--filter …]` |
| `dotnet-build` | `dotnet build <project> -v q` |
| `dotnet-format` | `dotnet format EventHub.slnx --verify-no-changes --include <path>` |
| `eslint` | `yarn --cwd web eslint <file> --max-warnings 0` |

If web `.ts/.tsx` changed: `yarn --cwd web exec tsc -b --noEmit`.

### 3d. Optimize — loop until green

**Still red → return to 3b.** Do not mark task done. Do not declare session complete.

Only when all steps exit 0:

- Mark task `[x]` in plan
- Update `progress.md` (**Status**, **Decisions**, **Next**)

### 3e. Continue

Next task without per-task confirmation unless blocked.

If blocked: `[SKIP]` + reason in plan; stop unless user says continue.

## Step 4: Finish

- Full sweep: `dotnet test EventHub.slnx`; `yarn --cwd web lint`; `yarn --cwd web build` if UI touched
- Optional Reflexion: invoke **`code-reviewer`** subagent on the diff
- **Delete** plan file when all tasks succeed
- **Do not** commit unless user asks · **Do not** update GitHub issues

## Examples

| Command | Action |
|---------|--------|
| `/build .cursor/plans/20260614143000-place-order.md` | Full plan |
| `/build docs/specs/20260614143000-place-order.md` | Resolves paired plan |
| `/build … task 2` | Task 2 onward |

## DO NOT

- Self-grade completion without green tests/type-check
- Commit or stage `.cursor/plans/**`
- Stop after one task unless blocked
- Edit `web/src/generated/` by hand
