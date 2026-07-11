---
name: backend
description: "Procedural backend workflow for EventHub .NET work: source-driven scouting, layer-specific AGENTS routing, Domain/Application/Infrastructure/Api/Contracts/AppHost changes, EF Core/PostgreSQL, MediatR/CQRS, System.Threading.Channels integration-event publisher/consumer work, backend tests, Aspire-aware verification, and OpenAPI handoff without duplicating repository rules."
---

# Backend Skill

Use this skill before backend implementation or review under `src/`, backend tests under `tests/`, or backend-owned REST contract work under `contracts/openapi/`.

## Contract With AGENTS

Treat this skill as workflow only. Repository rules, invariants, and source-of-truth decisions live in `AGENTS.md`, nested `AGENTS.md` files, and `docs/product.md`, `docs/features.md`, `docs/technical.md`.

When this skill appears to conflict with those files or with current code, follow the higher-precedence source and report the drift. Do not copy durable rules from `AGENTS.md` into this skill.

## Read Route

1. Read root `AGENTS.md`.
2. Read every scoped `AGENTS.md` for paths you may edit:
   - `src/AGENTS.md` for any backend source change.
   - `src/Domain/AGENTS.md`, `src/Application/AGENTS.md`, `src/Infrastructure/AGENTS.md`, `src/Api/AGENTS.md`, `src/Contracts/AGENTS.md` as applicable.
   - `contracts/AGENTS.md` for public REST shape changes.
   - `tests/AGENTS.md` for backend test changes.
3. Read the smallest relevant sections of `docs/technical.md`; add `docs/features.md` for observable behavior and `docs/product.md` for scope, pricing, payment/data handling, fairness, or guardrail concerns.
4. Read nearby implementation and test files before editing.

## Source Scout

Prefer `rg`/`rg --files` to learn the current implementation from source:

```powershell
rg -n "sealed record .*Command|sealed record .*Query|class .*Handler" src/Application
rg -n "class .*Endpoint|interface IEndpoint|MapEndpoints" src/Api
rg -n "ApplicationDatabaseContext|IEntityTypeConfiguration|DbSet<|Migration" src/Infrastructure
rg -n "record struct .*Id|BusinessRuleValidationException|AddDomainEvent" src/Domain
rg -n "AssertValidationFailedAsync|IntegrationTestFixture|ShouldBe" tests
```

Open the concrete files you find. Let local naming, result mapping, builders, fixtures, and registration patterns drive the change.

## Workflow

1. Classify the change by layer and risk: domain invariant, application orchestration, persistence/adapter, messaging adapter, HTTP contract, AppHost/runtime, or tests.
2. Load only the applicable nested instructions and reference below.
3. Trace the full vertical slice when behavior crosses layers: endpoint/contract -> command/query -> handler -> domain method -> repository/adapter -> tests.
4. Plan intended files, invariants/acceptance criteria, verification command, rollback point, and approval boundary.
5. For bugs or risky behavior, write or update a focused failing test first when feasible.
6. Edit narrowly and preserve existing project patterns.
7. Run focused checks, then broaden with the repo verifier before handoff when the change crosses layers.

## Reference Routing

Read only the reference needed for the current task:

- `references/architecture.md` - source-discovery map for a backend vertical slice.
- `references/endpoints.md` - endpoint and public REST contract workflow.
- `references/entities-dbcontext.md` - EF Core model/repository workflow.
- `references/messaging.md` - Channel-based integration event publisher, consumer, event, and DI workflow.
- `references/querying.md` - read/query workflow and translation-sensitive checks.
- `references/migrations.md` - migration generation and review workflow.
- `references/testing.md` - backend test placement and fixture discovery.
- `references/coverage.md` - risk-based test selection.

Use `openapi-contract-sync` for REST shape drift, `aspire-mcp` for running AppHost inspection, and `e2e` only when behavior must be proven in a browser journey.

## Verification

Plan first when the touched surface is unclear:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1 -PlanOnly
```

Run the changed-code verifier before handoff unless the task is read-only:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

Focused backend checks while iterating, adjusted to touched files:

```powershell
dotnet build EventHub.slnx -c Release
dotnet test tests/Domain.UnitTests/EventHub.Domain.UnitTests.csproj -c Release
dotnet test tests/Api.IntegrationTests/EventHub.Api.IntegrationTests.csproj -c Release
```

For public REST shape changes, use `openapi-contract-sync` and include:

```powershell
yarn --cwd web api:verify
```
