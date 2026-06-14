# Constitution

**Project:** Clean Architecture + CQRS + DDD Boilerplate  
**Status:** Immutable principles  
**Last Updated:** June 14, 2026

---

## Purpose

This document defines the **non-negotiable invariants** of the repository. Every design choice, code change, and agent workflow must comply with these principles.

When guidance conflicts:

1. **This constitution** wins over all other documents and Cursor rules.
2. **Product and technical docs** (`PRD.md`, `TECHNICAL.md`) win over scoped Cursor rules.
3. **Accepted decisions** in [`memory/decisions.md`](memory/decisions.md) win over informal notes or session artifacts.

Fix contradictions in lower-level docs or rules — do not weaken these principles without an explicit constitution amendment.

---

## I. Architecture

### 1. Clean Architecture is mandatory

Layers and dependency direction are fixed:

```
Domain ← Application ← Infrastructure
              ↑
             Api (composition root)
```

| Layer | May reference | Must never reference |
|-------|---------------|----------------------|
| Domain | — | EF Core, ASP.NET, MediatR, Infrastructure |
| Application | Domain, Contracts | Infrastructure, HTTP |
| Infrastructure | Application, Domain | Api |
| Api | Application, Infrastructure, Contracts | Domain logic in endpoints |

**Api is the composition root.** Inner layers never reference outer layers.

### 2. Domain is pure C#

Business rules live in `Solution.Domain` as framework-free C#. No ORM attributes, no HTTP types, no MediatR in Domain.

Aggregates enforce invariants internally. **No anemic domain models** — behavior belongs on aggregates and value objects, not scattered in handlers.

### 3. Sample bounded context is replaceable; patterns are not

The `User` aggregate demonstrates DDD tactical patterns. Replace the sample domain with your own bounded contexts, but keep the same layering, CQRS, and port/adapter boundaries.

---

## II. Application and CQRS

### 4. Commands and queries are separated

- **Commands** mutate state and implement `ICommand` / `ICommand<T>`.
- **Queries** read state and implement `IQuery<T>`.
- Handlers return `Result` or `Result<T>`.

All use cases flow through **MediatR** in Application — not through Api controllers with embedded logic.

### 5. MediatR pipeline order is fixed

Pipeline behaviors run in this order:

1. `DomainEventDispatchBehavior` — after successful handler
2. `ValidationBehavior` — FluentValidation before handler
3. `LoggingBehavior`
4. `UnitOfWorkBehavior` — transaction + optimistic concurrency retry
5. `PostCommitSessionCacheBehavior` — Redis cache after commit

Do not bypass the pipeline for writes that need transactions or domain events.

### 6. Ports in Application; adapters in Infrastructure

Application defines abstractions (repositories, unit of work, external services). Infrastructure implements them. Handlers depend on ports, never on concrete EF or Redis types.

### 7. Ownership and authorization live in Application

HTTP middleware establishes session identity. **Authorization and ownership checks belong in command/query handlers**, not in Api endpoints alone.

---

## III. Data and persistence

### 8. PostgreSQL is the source of truth

All authoritative state persists in PostgreSQL (`app` schema). Redis holds **rebuildable cache only** — never the sole copy of business data.

Session cache is written **after** PostgreSQL commit, never before.

### 9. Storage follows aggregate boundaries

- One primary table per aggregate root.
- Declarative constraints enforce invariants at the database where possible.
- Mutable aggregates use optimistic concurrency (`row_version`).
- Timestamps are stored as **UTC** (`TIMESTAMPTZ`).

### 10. Migrations are append-only

- Generate migrations via EF Core tooling in `src/Infrastructure/Migrations/`.
- **Never edit merged migrations** — add a new migration instead.
- Schema changes must stay aligned with [`TECHNICAL.md`](TECHNICAL.md) §6 and EF configurations.

---

## IV. API and contracts

### 11. Api is a thin HTTP surface

- Endpoints implement `IEndpoint` and are discovered via assembly scan.
- Endpoints send MediatR requests and map to **Contracts DTOs** — never serialize domain entities.
- **MediatR only** in the Api layer for use-case dispatch.

### 12. REST and error conventions are stable

- Success responses return the resource DTO directly — no custom `{ data, meta }` envelope.
- Errors use **RFC 7807** problem details (`ApiProblemDetails`) with stable `code` values.
- Two-layer validation: JSON binding → `400`; FluentValidation / domain rejection → `422`.
- **Never return `200` with an error body** — use the correct HTTP status.

Breaking changes to public HTTP routes require explicit discussion and contract updates.

### 13. OpenAPI contract is the API shape source of truth

REST shapes live in [`contracts/openapi/api.v1.yaml`](../contracts/openapi/api.v1.yaml). After endpoint changes:

1. Export from the API build.
2. Regenerate frontend types.
3. Verify in CI (`api:verify`).

Domain types must not appear in OpenAPI or JSON responses.

---

## V. Local development and orchestration

### 14. Aspire AppHost is the topology source of truth

Local orchestration runs through **.NET Aspire AppHost** — PostgreSQL, Redis, Api, and web (Vite). Do not add hand-authored `docker-compose.yml` for service orchestration.

### 15. ServiceDefaults is mandatory for Api

`Solution.ServiceDefaults` provides shared logging, health checks, and service discovery. Api must use it.

### 16. Configuration layering is fixed

Later sources override earlier ones:

`appsettings.json` → `appsettings.Development.json` → Aspire environment → user secrets

Connection strings are Aspire-injected: `ConnectionStrings__app`, `ConnectionStrings__cache`.

---

## VI. Code conventions

### 17. Naming and discovery standards

- **No XML doc comments** (`/// <summary>`) — use clear type and member names.
- **No abbreviations** in type, method, property, file, or parameter names. Exceptions: framework terms (`DbContext`, `Guid`) and official library API names.
- **No `Trading` prefix** on types — the solution namespace is already `Solution.*`.
- Every project under `src/` and `tests/` includes **`AssemblyReference.cs`**. Use `AssemblyReference.Assembly` for MediatR, FluentValidation, EF configuration, and endpoint discovery — not `typeof(DependencyInjection).Assembly` or `Assembly.GetEntryAssembly()`.

### 18. File size and quality bar

- Prefer files **≤ 500 lines**; split when a type or handler grows unwieldy.
- Significant architecture choices require an entry in [`memory/decisions.md`](memory/decisions.md).

---

## VII. Testing

### 19. Selective, meaningful tests only

- **Domain unit tests** — pure aggregate and value object behavior, no DI.
- **Api integration tests** — HTTP surface with Testcontainers (PostgreSQL, Redis).
- Integration tests use fakes at Application **ports**, not domain mocks.
- No coverage targets; do not add tests that only assert the obvious.

---

## VIII. Repository layout

Fixed top-level structure:

```
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts
tests/     Domain.UnitTests, Api.IntegrationTests, Testing.Common
web/       React 19 + Vite (outside .slnx; Yarn; run via Aspire web resource)
docs/      PRD, TECHNICAL, memory/, this constitution
contracts/ OpenAPI contract and codegen scripts
.cursor/   Rules, skills, commands for Cursor agents
```

Do not collapse layers into monolithic projects or move orchestration outside AppHost.

---

## IX. Explicitly out of scope

Unless explicitly requested and documented as a decision, the following are **not part of this boilerplate**:

- Production deployment and CD pipelines
- Message brokers and transactional outbox
- Multi-tenancy
- Horizontal scaling patterns
- Advanced observability beyond Aspire defaults

Adding these requires a new architecture decision and must not violate principles I–VIII.

---

## X. Amendment process

To change an invariant in this document:

1. Propose the change with rationale in [`memory/decisions.md`](memory/decisions.md).
2. Update affected docs (`PRD.md`, `TECHNICAL.md`, Cursor rules) to match.
3. Amend this constitution in the same change set.

Silent drift — code or rules that contradict this document without amendment — is a defect.

---

## Document map

| Document | Role |
|----------|------|
| **This file** | Immutable principles |
| [`PRD.md`](PRD.md) | Boilerplate scope and included patterns |
| [`TECHNICAL.md`](TECHNICAL.md) | Architecture, CQRS, API, persistence, testing |
| [`memory/current-status.md`](memory/current-status.md) | Active work and session checklist |
| [`memory/decisions.md`](memory/decisions.md) | Accepted architecture decisions |
| [`memory/known-issues.md`](memory/known-issues.md) | Documented bugs and workarounds |
