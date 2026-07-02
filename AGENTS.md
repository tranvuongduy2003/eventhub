# EVENTHUB - CORE

EventHub is a Clean Architecture + CQRS + DDD event management and ticketing platform. Local topology is .NET Aspire only.

## Source Of Truth

Read the smallest relevant set before acting. Do not load overlapping skills when these docs already cover the task.

| Document | Use For |
|---|---|
| `docs/CONSTITUTION.md` | Non-negotiable architecture, data, API, local run, naming, testing |
| `docs/_memory/source/product-requirements.md` | Product intent, personas, scope, decisions (`DEC-*`), guardrails (`QG-*`) |
| `docs/_memory/source/feature-specification.md` | Epics (`EP-*`), features (`F-*`), acceptance criteria, build order |
| `docs/_memory/source/domain-model-specification.md` | Bounded contexts, aggregates, invariants, domain/integration events |
| `docs/_memory/source/technical-design.md` | Layer layout, CQRS, infrastructure, persistence, API, testing |
| `docs/_memory/source/harness-architecture.md` | Codex harness boundaries and structure |
| `docs/_memory/source/harness-operational-policies.md` | Hooks, policy, state, verification, eval loop |

Precedence: Constitution -> source memory (`docs/_memory/source/`) -> harness docs -> scoped rule -> skill. Fix lower-level drift in a follow-up change.

## Non-Negotiables

1. Domain -> Application -> Infrastructure; Api is the composition root.
2. `EventHub.Domain` is pure C#: no EF Core, ASP.NET, MediatR, or Infrastructure.
3. Aspire AppHost is the local topology source of truth. Do not add hand-authored `docker-compose.yml`.
4. `EventHub.ServiceDefaults` is mandatory for Api.
5. PostgreSQL is authoritative. Redis is rebuildable cache only. MinIO stores binary assets.
6. Domain model follows `docs/_memory/source/domain-model-specification.md`: modular monolith, RabbitMQ integration events across bounded contexts.
7. Commands and queries stay separated in Application and flow through MediatR.
8. API endpoints are thin and return Contracts DTOs, never domain entities.
9. OpenAPI shape lives in `contracts/openapi/api.v1.yaml`; do not hand-edit `web/src/generated/`.
10. Tests are meaningful and selective: Domain unit tests, Api integration tests, Playwright e2e when required.

## Repository Layout

```text
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts, DataSeeder
tests/     Domain.UnitTests, Api.IntegrationTests, Testing.Common
e2e/       Playwright e2e tests
web/       React + Vite; Yarn; run via Aspire web resource
docs/      Obsidian knowledge memory, constitution, source memory, specs, harness docs
contracts/ OpenAPI contract and codegen scripts
.codex/    Codex config, hooks, policies, custom agents
.agents/   repo-local skills
.graph/    path-to-verification map
evals/     deterministic harness/graph/agent evals
harness/   future orchestration runtime scaffold
```

## Harness Contract

Harness means the policy and orchestration layer around agent work, not ad-hoc prompt examples.

- Policy data: `.codex/policies/harness-policy.json`
- Lifecycle hooks: `.codex/hooks/`
- Runtime state: `.codex/state/` (gitignored)
- Path-to-check graph: `.graph/index.json`
- Deterministic evals: `evals/run.ps1`
- Stable agent scripts: `scripts/agent/`

Default verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

Run after harness or hook changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File evals/run.ps1 -Layer harness
```

## Skill Routing

Use repo docs first. Use a skill only for the procedure it owns.

| Trigger | Skill |
|---|---|
| Fresh workspace or broken local setup | `repo-bootstrap` or `env-doctor` |
| Verify current diff | `verify-changed-code` |
| Final review handoff | `pr-handoff` |
| Product spec | `spec` |
| Engineering plan from a spec | `plan` |
| Implement an existing plan | `cook` |
| OpenAPI export/codegen/CI verify | `openapi-contract-sync` |
| Live DB read-only SQL | `postgres-mcp` |
| Neo4j graph / GraphRAG | `neo4j-graphrag` |
| Playwright e2e | `playwright-e2e` |
| Frontend server state | `tanstack-query` |
| shadcn / Tailwind UI | `shadcn`, `tailwind-patterns` |
| Commits, PRs, GitHub | `git-commit-writer`, `pr-description-writer`, `create-pr`, `github-cli` |

Do not treat kubectl/goclaw-style examples as core harness components. They belong only as future CLI/skill standards when this repo actually needs those tools.

## Subagents

Prefer project subagents over generic exploration.

| Agent | Use | Writes? |
|---|---|---|
| `codebase-explorer` | Path/line scout before edits | no |
| `plan-domain-researcher` | Domain planning | no |
| `plan-application-researcher` | CQRS/Application planning | no |
| `plan-infrastructure-researcher` | EF/Api/contracts/tests planning | no |
| `plan-web-researcher` | Frontend planning | no |
| `graph-impact-analyst` | Blast radius | no |
| `test-impact-analyzer` | Diff-to-tests scope | no |
| `build-test-writer` | Bug path red test first | tests only |
| `e2e-test-writer` | Feature path red e2e first | e2e only |
| `code-reviewer` | Evidence-based review after substantial work | no |

Parallelize only read-only scouts. Parent agent owns production code edits.

## Workflow

Use ReAct: verify context, act, observe the result. Do not declare done without objective checks.

For implementation:

1. Read the relevant source docs.
2. Scout code paths.
3. Edit narrowly.
4. Run affected checks.
5. For substantial work, run review or record why it was skipped.
6. Handoff with changed files, tests run, and residual risk.

Bug fixes should get a red test first when feasible. Do not add low-value tests that assert the obvious.

## Context Memory

Long tasks should use durable local notes instead of chat memory:

- `.codex/plans/` for ephemeral implementation plans; never commit.
- `.codex/notes/progress.md` for local progress; gitignored.
- `.codex/agent-memory/*.md` for worker memory; gitignored.

Record only decisions, changed files, blockers, and next steps. Do not dump command output.

## Protected Actions

Hooks and policy block common hazards, but the working rules remain:

- Do not edit `.env`, `.env.*`, `.mcp.json`, `web/src/generated/`, or generated OpenAPI build output.
- Do not run destructive git operations.
- Do not add production dependencies without explicit approval.
- Do not use npm in `web/`; use Yarn.
- Do not hand-author Docker Compose for local service topology.
