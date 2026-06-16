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

## OpenCode rules (native)

**Always loaded** (via `opencode.json` → `instructions`): `AGENTS.md`, this file, `harness.md`, `reasoning-loop.md`, `context-memory.md`.

**Read on demand** — use the **read** tool for the matching file under `.opencode/rules/` when the task matches (do not preload every scoped rule):

| File | When |
|------|------|
| `.opencode/rules/architecture.md` | Layers, CQRS, DDD, backend structure |
| `.opencode/rules/backend.md` | .NET stack conventions |
| `.opencode/rules/backend-testing.md` | Test layout and patterns |
| `.opencode/rules/api-guidelines.md` | HTTP, errors, OpenAPI |
| `.opencode/rules/migration.md` | EF Core, schema changes |
| `.opencode/rules/aspire.md` | AppHost, local run |
| `.opencode/rules/frontend.md` | React, TanStack Query |
| `.opencode/rules/design-system.md` | UI tokens, shadcn |
| `.opencode/rules/spec-artifacts.md` | `/spec`, `/plan`, `/build` artifacts |
| `.opencode/rules/agent-stack.md` | Five-layer walkthrough example |

## DOC ROUTING (prefer over skills)

| Work | Read first |
|------|------------|
| Architecture, layers, dependencies | Constitution I · `.opencode/rules/architecture.md` · Tech §1–3 |
| Domain / aggregates / BC map | Constitution I.2–3 · `ddd.md` · `.opencode/rules/architecture.md` §3 |
| Feature scope / acceptance | `features.md` (feature id) · `prd.md` (intent) |
| CQRS, handlers, pipeline | Constitution II · `.opencode/rules/architecture.md` §4–5 · Tech §4 |
| EF Core, schema, migrations | Constitution III · Tech §6 · `.opencode/rules/migration.md` |
| HTTP, errors, auth, SignalR | Constitution IV · Tech §7 · `.opencode/rules/api-guidelines.md` |
| Aspire / local run | Constitution V · Tech §8–10 · `.opencode/rules/aspire.md` |
| Naming, file size, quality | Constitution VI |
| Tests | Constitution VII · Tech §11 · `.opencode/rules/backend-testing.md` |
| Frontend (web/) | `.opencode/rules/frontend.md`, `.opencode/rules/design-system.md` |

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
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts
tests/     Domain.UnitTests, Api.IntegrationTests, Testing.Common  (see backend-testing.md)
web/       React 19 + Vite (not in .slnx; Yarn; run via Aspire `web` Vite app)
docs/      constitution, prd, features, ddd, technical, specs/
.opencode/   rules/, skills/, commands/, hooks/, notes/, agent-memory/, agents/
.graph/    index.json — path→test map for agent harness (extend to full graph)
evals/     cases/, fixtures/, run.ps1 — objective eval pipeline (harness + graph + agent)
```

## PROCEDURAL SKILLS (when docs are not enough)

Read `.opencode/skills/<name>/SKILL.md` only for these workflows:

| Trigger | Skill |
|---------|--------|
| OpenAPI export / codegen / CI verify | `openapi-contract-sync` |
| Live DB inspection (read-only SQL via MCP) | `postgres-mcp` |
| Neo4j graph / GraphRAG queries via MCP | `neo4j-graphrag` |
| Local env broken | `env-doctor` |
| Frontend server state | `tanstack-query` |
| shadcn / Tailwind UI | `shadcn`, `tailwind-patterns` |
| Commits / PRs / GitHub | `create-pr`, `git-commit-writer`, `pr-description-writer`, `github-cli` |
| Hooks blocked / verify gate / stop loop | `harness.md` (always on) |
| Context compaction / progress notes | `context-memory.md` |
| ReAct / Reflexion / subagent delegation | `reasoning-loop.md` |
| How layers compose (worked example) | `agent-stack.md` |

## SLASH COMMANDS (`.opencode/commands/`)

| Command | When | Outcome | Then |
|---------|------|---------|------|
| `/spec` | New feature slice; need product spec | `docs/specs/<timestamp>-<name>.md` | `/plan` |
| `/plan` | Spec exists; need engineering plan | `.opencode/plans/<same-filename>.md` (never commit) | `/build` |
| `/build` | Plan exists; implement | Code + green tests/type; delete plan when done | PR / review |

Read the matching command file before executing. `/plan` and `/build` delegate to project subagents below.

## PROJECT SUBAGENTS (`.opencode/agents/<name>.md`)

OpenCode has **primary** agents (`build`, `plan` — switch with Tab) and **subagents** (built-in: `explore`, `general`, `scout`; plus project agents below).

Invoke project subagents via **`@<agent>`** in chat or the **`task`** tool from the **build** primary agent. Prefer project scouts over built-in `@explore` when EventHub paths matter.

| Agent | When | `permission.edit` | parallel OK |
|-------|------|-------------------|-------------|
| `codebase-explorer` | Before edits — path:line scout | deny | yes |
| `plan-domain-researcher` | `/plan` — Domain aggregates, `INV-*` | deny | yes |
| `plan-application-researcher` | `/plan` — CQRS handlers, ports | deny | yes |
| `plan-infrastructure-researcher` | `/plan` — EF, Api, integration tests | deny | yes |
| `plan-web-researcher` | `/plan` — web routes, Query, UI | deny | yes |
| `graph-impact-analyst` | `/plan` or `/build` — blast radius (neo4j-graphrag MCP) | deny | yes |
| `test-impact-analyzer` | git diff → affected tests + gaps | deny | yes |
| `build-test-writer` | `/build` bug path — red test before fix | allow (tests only) | **no** |
| `code-reviewer` | After `/build` — Reflexion with command output | deny | no |

**Topology:** parallel only for readonly scouts; **parent agent** writes production code alone. `reasoning-loop.md` Layer 5.

Worker memory: `.opencode/agent-memory/<name>.md` (durable codepaths; parent uses `.opencode/notes/progress.md`).

## WHEN TO CONSULT WHICH RULE

| Concern | Rule |
|---|---|
| Architecture, CQRS, DDD, layers | `architecture.md` |
| .NET services (stack, DON'Ts) | `backend.md` |
| Tests in `tests/**` | `backend-testing.md` |
| React UI | `frontend.md` |
| HTTP / API contracts | `api-guidelines.md` |
| EF / PostgreSQL schema | `migration.md` |
| AppHost / local run | `aspire.md` |
| Implementation specs (durable) | `spec-artifacts.md` |
| Agent hooks / verify gate | `harness.md` |
| Session notes / compaction | `context-memory.md` |
| ReAct / done criteria | `reasoning-loop.md` |
| Five-layer walkthrough (example) | `agent-stack.md` |

## OUT OF SCOPE (unless explicitly requested)

Production CD, transactional outbox, multi-tenancy, horizontal scaling, enterprise venue features, multi-currency, paid secondary marketplace (see `prd.md` §6.2).
