# EVENTHUB — CORE

**EventHub** — Clean Architecture + CQRS + DDD event management and ticketing. Local-only via .NET Aspire.

## SOURCE OF TRUTH (read before acting)

**Do not load overlapping skills when these docs cover the task.** Skills are for procedures not spelled out below.

| Document | When to consult |
|---|---|
| [`docs/constitution.md`](docs/constitution.md) | Non-negotiable invariants (architecture, CQRS, data, API, local run, naming) |
| [`docs/prd.md`](docs/prd.md) | Product intent, personas, goals, decisions (`DEC-*`), guardrails (`QG-*`) |
| [`docs/features.md`](docs/features.md) | Epics (`EP-*`), features (`F-*`), acceptance criteria, build order |
| [`docs/ddd.md`](docs/ddd.md) | Bounded contexts, aggregates, invariants, domain/integration events |
| [`docs/technical.md`](docs/technical.md) | Layer layout, CQRS pipeline, infrastructure, persistence §6, API §7, testing §11 |

**Cross-reference:** `Constitution §N`, `PRD §N` / `DEC-*`, `F-*`, `Tech §N`, `ddd.md §N`.

When a **rule**, **skill**, or **doc** disagree, precedence is: **Constitution → prd / features / ddd / technical → scoped rule → skill**. Fix the lower layer in a follow-up PR.

## DOC ROUTING (prefer over skills)

| Work | Read first |
|------|------------|
| Architecture, layers, dependencies | Constitution I · `architecture.md` · Tech §1–3 |
| Domain / aggregates / BC map | Constitution I.2–3 · `ddd.md` · `architecture.md` §3 |
| Feature scope / acceptance | `features.md` (feature id) · `prd.md` (intent) |
| CQRS, handlers, pipeline | Constitution II · `architecture.md` §4–5 · Tech §4 |
| EF Core, schema, migrations | Constitution III · Tech §6 · `migration.md` |
| HTTP, errors, auth, SignalR | Constitution IV · Tech §7 · `api-guidelines.md` |
| Aspire / local run | Constitution V · Tech §8–10 · `aspire.md` |
| Naming, file size, quality | Constitution VI |
| Tests | Constitution VII · Tech §11 · `backend-testing.md` |
| E2E tests (Playwright) | `e2e-testing.md` · `playwright-e2e` skill |
| Frontend (web/) | `frontend.md`, `design-system.md` |

## NON-NEGOTIABLE INVARIANTS

Summarized here for quick scan; **Constitution is authoritative** if anything differs.

1. **Clean Architecture** — Domain → Application → Infrastructure; Api is the composition root.
2. **Domain is pure C#** — no EF Core, ASP.NET, MediatR, or Infrastructure in `EventHub.Domain`.
3. **Aspire AppHost** is the local topology source of truth — no hand-authored `docker-compose.yml`.
4. **`EventHub.ServiceDefaults` is mandatory** for Api.
5. **PostgreSQL is authoritative**; Redis holds rebuildable cache only; MinIO for binary assets.
6. **Domain model** follows [`docs/ddd.md`](docs/ddd.md) — modular monolith, RabbitMQ for cross-context integration events.

## REPOSITORY LAYOUT

```
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts, DataSeeder
tests/     Domain.UnitTests, Api.IntegrationTests, Testing.Common  (see backend-testing.md)
e2e/       Playwright e2e tests (Yarn; see e2e-testing.md)
web/       React 19 + Vite (not in .slnx; Yarn; run via Aspire `web` Vite app)
docs/      constitution, prd, features, ddd, technical, specs/
.claude/   rules/, skills/, commands/, hooks/, notes/, agent-memory/, agents/
.graph/    index.json — path→test map for agent harness (extend to full graph)
evals/     cases/, fixtures/, run.ps1 — objective eval pipeline (harness + graph + agent)
```

## PROCEDURAL SKILLS (when docs are not enough)

Read `.claude/skills/<name>/SKILL.md` only for these workflows:

| Trigger | Skill |
|---------|--------|
| OpenAPI export / codegen / CI verify | `openapi-contract-sync` |
| Live DB inspection (read-only SQL via MCP) | `postgres-mcp` |
| Neo4j graph / GraphRAG queries via MCP | `neo4j-graphrag` |
| Local env broken | `env-doctor` |
| Frontend server state | `tanstack-query` |
| shadcn / Tailwind UI | `shadcn`, `tailwind-patterns` |
| Commits / PRs / GitHub | `create-pr`, `git-commit-writer`, `pr-description-writer`, `github-cli` |
| Hooks blocked / verify gate / stop loop | See **Agent harness** section below |
| Playwright e2e tests | `playwright-e2e` |
| Context compaction / progress notes | See **Context memory** section below |
| ReAct / Reflexion / subagent delegation | See **Reasoning loop** section below |
| How layers compose (worked example) | `agent-stack.md` |

## SLASH COMMANDS (`.claude/commands/`)

| Command | When | Outcome | Then |
|---------|------|---------|------|
| `/spec` | New feature slice; need product spec | `docs/specs/<timestamp>-<name>.md` | `/plan` |
| `/plan` | Spec exists; need engineering plan | `.claude/plans/<same-filename>.md` (never commit) | `/build` |
| `/build` | Plan exists; implement | Code + green tests/type; delete plan when done | PR / review |

Read the matching command file before executing. `/plan` and `/build` delegate to project subagents below.

## PROJECT SUBAGENTS (`.claude/agents/<name>.md`)

Invoke with `@agent-<name>` or `Agent(<name>)`. **Do not** substitute generic `Explore`.

| name | When | readonly | parallel OK |
|------|------|----------|-------------|
| `codebase-explorer` | Before edits — path:line scout; no dumps | yes | yes |
| `plan-domain-researcher` | `/plan` — Domain aggregates, `INV-*`, events | yes | yes |
| `plan-application-researcher` | `/plan` — CQRS commands/queries/handlers/ports | yes | yes |
| `plan-infrastructure-researcher` | `/plan` — EF, Api, integration tests | yes | yes |
| `plan-web-researcher` | `/plan` — web routes, TanStack Query, UI | yes | yes |
| `graph-impact-analyst` | `/plan` or `/build` — blast radius (neo4j-graphrag MCP) | yes | yes |
| `test-impact-analyzer` | git diff → affected tests + coverage gaps | yes | yes |
| `build-test-writer` | `/build` bug path — red test before fix | no (tests only) | **no** |
| `e2e-test-writer` | `/build` TDD path — red e2e test before feature | no (tests only) | **no** |
| `code-reviewer` | After `/build` — Reflexion with command output | yes | no |

**Topology:** parallel only for readonly scouts; **parent agent** writes production code alone. See **Reasoning loop** Layer 5.

Worker memory: `.claude/agent-memory/<name>.md` (durable codepaths; parent uses `.claude/notes/progress.md`).

## WHEN TO CONSULT WHICH RULE

| Concern | Rule |
|---|---|
| Architecture, CQRS, DDD, layers | `architecture.md` |
| .NET services (stack, DON'Ts) | `backend.md` |
| Tests in `tests/**` | `backend-testing.md` |
| E2E tests in `e2e/**` | `e2e-testing.md` |
| React UI | `frontend.md` |
| HTTP / API contracts | `api-guidelines.md` |
| EF / PostgreSQL schema | `migration.md` |
| AppHost / local run | `aspire.md` |
| Implementation specs (durable) | `spec-artifacts.md` |
| Agent hooks / verify gate | See **Agent harness** below |
| Session notes / compaction | See **Context memory** below |
| ReAct / done criteria | See **Reasoning loop** below |
| Five-layer walkthrough (example) | `agent-stack.md` |

## OUT OF SCOPE (unless explicitly requested)

Production CD, transactional outbox, multi-tenancy, horizontal scaling, enterprise venue features, multi-currency, paid secondary marketplace (see `prd.md` §6.2).

---

# Context memory (compaction-safe)

Long tasks lose detail when Claude Code **compacts** context. Use durable files instead of relying on chat history.

## Session start

1. Read `.claude/notes/progress.md` if it exists; otherwise create it from `.claude/notes/progress.example.md`.
2. Align work with **Goal**, **Decisions**, and **Next** before new exploration.

## During work

Update `progress.md` after milestones: task completed, architecture decision, blocker found, or before delegating to a subagent.

**Write to Decisions:** approach chosen, files created/changed, spec/feature ids, anything you would regret losing after compact.

Do not dump tool output — bullets only.

## Subagents (Agent tool)

Before launching a subagent, add one line to `progress.md` **Status** (what the worker should do).

Project subagents (see subagent table above — e.g. `plan-domain-researcher`, `graph-impact-analyst`):

1. Read `.claude/agent-memory/<agent-name>.md` at start if present.
2. After non-trivial work, append **durable** codepaths/patterns only (under ~150 lines; no secrets).

**Worker memory format:** `# <agent-name>` · sections `Codepaths`, `Patterns`, `Gotchas` — bullets only.

Parent owns `progress.md`; each worker owns `agent-memory/<agent-name>.md`.

## After compaction

Re-read `progress.md` and latest `.claude/notes/backups/<timestamp>/` (transcript + notes from PreCompact hook).

---

# Agent harness (hooks)

Deterministic enforcement in `.claude/settings.json` — not prompt advice.

## Hook map

| Event | Script | Behavior |
|-------|--------|----------|
| `PreToolUse` | `pre-tool-guard.ps1` | Deny Write to `web/src/generated/`, `.env`, `.mcp.json`; deny dangerous Shell |
| `PreToolUse` (Bash) | `before-shell-guard.ps1` | Deny `rm -rf`, `git push --force`, `git reset --hard`, `git config`, `npm install` |
| `PostToolUse` | `post-edit-verify.ps1` | lint/format + affected test/build via `scripts/affected-tests.mjs`; sets verify gate on fail |
| `Stop` | `stop-gate.ps1` | Block if verify gate active or git-diff checks fail (tsc + affected steps) |
| `PreCompact` | `pre-compact-backup.ps1` | Backup transcript + `progress.md` to `.claude/notes/backups/` |

## Verify gate

Failure writes `.claude/hooks/state/verify-gate.json` → blocks `Shell`, `Agent`, `Delete` until next green edit verification. `Write`/`Read`/`Grep` still allowed.

Clear manually: delete `verify-gate.json`.

## Affected checks

| Changed path | Checks |
|--------------|--------|
| `web/**/*.{ts,tsx}` | eslint + tsc at stop if web touched |
| `src/Domain/**` | dotnet format + filtered Domain.UnitTests |
| `src/Application\|Infrastructure\|Api\|Contracts\|DataSeeder/**` | dotnet format + layer build |
| Map source | `.graph/index.json` + `node scripts/affected-tests.mjs <path>` |

## Regression

After hook changes: `.\evals\run.ps1` (CI job `agent-evals`).

---

# Reasoning loop (Layer 3)

## ReAct (default for every task)

Alternate **reason → act → observe** — do not batch many tools without reading results.

1. **Reason** — one short intent: what you will verify or change and why.
2. **Act** — one tool (or one logical edit batch).
3. **Observe** — read exit code, linter output, test result, file content; update `.claude/notes/progress.md` **Decisions** if the outcome matters after compaction.

Do not declare understanding until you have observed the tool result. Do not chain speculative edits across layers without a build/test signal in between.

## Reflexion (before "done")

Self-critique without external signal is unreliable. **Done** is allowed only when objective checks pass:

| Gate | What runs |
|------|-----------|
| `PostToolUse` | lint/format + affected test/build per changed file |
| `Stop` (`stop-gate.ps1`) | TypeScript (`tsc -b --noEmit`) if web changed; affected checks on full `git diff` |
| Verify gate | Blocks Bash/Agent until post-edit checks pass |

If the agent tries to stop while checks fail, the Stop hook sends a **followup_message** — treat it as ground truth, fix, re-run checks, then continue.

After substantial implementation, delegate **`code-reviewer`** via `@agent-code-reviewer` or `Agent(code-reviewer)`.

For `/plan`, launch parallel **readonly** workers only — see subagent table above.

## Orchestration topology (Layer 5)

> **Multi-agent for parallel read-only research; single agent for writing code** where decisions couple.

| Mode | Who | Parallel? |
|------|-----|-----------|
| Read / scout | `codebase-explorer`, `plan-*-researcher`, `graph-impact-analyst`, `test-impact-analyzer` | **Yes** — isolated windows, summaries back to parent |
| Write code | **Parent agent only** | **No** — never spawn multiple writers on the same change |
| Write tests (bug path) | `build-test-writer` then parent fix | **Sequential** — test writer first, then parent production code |
| Review | `code-reviewer` after parent finishes | Sequential |

**Golden rule:** parallel Agent calls must all target **readonly** subagents. If two subagents would make conflicting implementation choices, keep one writer (the parent).

Built-in `Explore` is a fallback only when no project subagent fits — prefer `codebase-explorer` or layer-specific `plan-*-researcher`.

## Not default

**Tree-of-Thoughts** — only for explicit multi-branch search problems (architecture options, complex bug hypotheses). Not for routine feature work.

## Workflow tie-in

- `/build` completion: `dotnet test`, `yarn --cwd web lint`, update `progress.md` **Next** checkboxes.
- Before PR: `.\evals\run.ps1` when hooks or graph changed.
- Worked example (all layers): `agent-stack.md` — add `Phone` to `User`.
