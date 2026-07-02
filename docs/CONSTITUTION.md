# Constitution

**Project:** EventHub — event management and ticketing platform  
**Status:** Immutable principles  
**Last Updated:** June 14, 2026

---

## Purpose

This document defines the **non-negotiable invariants** of the repository. Every design choice, code change, and agent workflow must comply with these principles.

When guidance conflicts:

1. **This constitution** wins over all other documents and Codex rules.
2. **Product and technical source memory** ([[_memory/source/product-requirements|product requirements]], [[_memory/source/feature-specification|feature specification]], [[_memory/source/domain-model-specification|domain model specification]], [[_memory/source/technical-design|technical design]]) wins over scoped Codex rules.
3. **Resolved product decisions** in [[_memory/source/product-requirements|product requirements]] (`DEC-*`) win over informal notes or session artifacts.

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

Business rules live in `EventHub.Domain` as framework-free C#. No ORM attributes, no HTTP types, no MediatR in Domain.

Aggregates enforce invariants internally. **No anemic domain models** — behavior belongs on aggregates and value objects, not scattered in handlers.

The domain model — bounded contexts, aggregates, invariants, events — is specified in [[_memory/source/domain-model-specification|domain model specification]]. Code must stay aligned with that document.

### 3. Modular monolith

EventHub runs as a **single deployable** with **logical bounded contexts** inside one Domain project (see [[_memory/source/domain-model-specification|domain model specification]] §2). Do not split into microservices unless a future constitution amendment explicitly allows it.

Cross-context integration follows [[_memory/source/technical-design|technical design]] §4–5: in-process domain events within a context; **RabbitMQ integration events** across contexts with idempotent consumers.

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

Application defines abstractions (repositories, unit of work, payment gateway, email, storage, messaging). Infrastructure implements them. Handlers depend on ports, never on concrete EF, Redis, MinIO, or RabbitMQ types.

### 7. Ownership and authorization live in Application

HTTP middleware establishes session identity. **Authorization and ownership checks belong in command/query handlers**, not in Api endpoints alone.

---

## III. Data and persistence

### 8. PostgreSQL is the source of truth

All authoritative state persists in PostgreSQL (`app` schema). Redis holds **rebuildable cache only** — never the sole copy of business data.

Session cache is written **after** PostgreSQL commit, never before.

Binary assets (cover images, avatars) are stored in **MinIO**; PostgreSQL holds references only.

### 9. Storage follows aggregate boundaries

- One primary table per aggregate root (see [[_memory/source/domain-model-specification|domain model specification]] and [[_memory/source/technical-design|technical design]] §6).
- Declarative constraints enforce invariants at the database where possible.
- Mutable aggregates use optimistic concurrency (`row_version`).
- Timestamps are stored as **UTC** (`TIMESTAMPTZ`).

### 10. Migrations are append-only

- Generate migrations via EF Core tooling in `src/Infrastructure/Migrations/`.
- **Never edit merged migrations** — add a new migration instead.
- Schema changes must stay aligned with [[_memory/source/technical-design|technical design]] §6 and EF configurations.

---

## IV. API and contracts

### 11. Api is a thin HTTP surface

- Endpoints implement `IEndpoint` and are discovered via assembly scan.
- Endpoints send MediatR requests and map to **Contracts DTOs** — never serialize domain entities.
- **MediatR only** in the Api layer for use-case dispatch.
- Realtime updates use **SignalR hubs** in Api (see [[_memory/source/technical-design|technical design]] §5).

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

Local orchestration runs through **.NET Aspire AppHost** — PostgreSQL, Redis, MinIO, RabbitMQ, Seq, Api, and web (Vite). Do not add hand-authored `docker-compose.yml` for service orchestration.

### 15. ServiceDefaults is mandatory for Api

`EventHub.ServiceDefaults` provides shared logging, health checks, and service discovery. Api must use it.

### 16. Configuration layering is fixed

Later sources override earlier ones:

`appsettings.json` → `appsettings.Development.json` → Aspire environment → user secrets

Connection strings and endpoints are Aspire-injected (e.g. `ConnectionStrings__app`, `ConnectionStrings__cache`).

---

## VI. Code conventions

### 17. Naming and discovery standards

- **No XML doc comments** (`/// <summary>`) — use clear type and member names.
- **No abbreviations** in type, method, property, file, or parameter names. Exceptions: framework terms (`DbContext`, `Guid`) and official library API names.
- Every project under `src/` and `tests/` includes **`AssemblyReference.cs`**. Use `AssemblyReference.Assembly` for MediatR, FluentValidation, EF configuration, and endpoint discovery — not `typeof(DependencyInjection).Assembly` or `Assembly.GetEntryAssembly()`.

### 18. File size and quality bar

- Prefer files **≤ 500 lines**; split when a type or handler grows unwieldy.
- Significant architecture choices that extend beyond [[_memory/source/product-requirements|product requirements]] decisions require a new `DEC-*` entry in [[_memory/source/product-requirements|product requirements]] §11 or an amendment to this constitution.

---

## VII. Testing

### 19. Selective, meaningful tests only

- **Domain unit tests** — pure aggregate and value object behavior, no DI.
- **Api integration tests** — HTTP surface with Testcontainers (PostgreSQL, Redis, RabbitMQ, MinIO as needed).
- Integration tests use fakes at Application **ports**, not domain mocks.
- No coverage targets; do not add tests that only assert the obvious.

---

## VIII. Repository layout

Fixed top-level structure:

```
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts
tests/     Domain.UnitTests, Api.IntegrationTests, Testing.Common
web/       React 19 + Vite (outside .slnx; Yarn; run via Aspire web resource)
docs/      Obsidian knowledge memory, constitution, source memory, specs, harness docs
contracts/ OpenAPI contract and codegen scripts
.codex/   Project config, hooks, MCP servers, custom agents
```

Do not collapse layers into monolithic projects or move orchestration outside AppHost.

**Durable workflow artifact:** specs in `docs/_memory/specs/` (`YYYYMMDDHHmmss-<name>.md`).

---

## IX. Explicitly out of scope

Unless explicitly requested and documented as a decision in [[_memory/source/product-requirements|product requirements]], the following are **not part of EventHub**:

- Production deployment and CD pipelines
- Transactional outbox (RabbitMQ is in scope; outbox pattern is not)
- Multi-tenancy
- Horizontal scaling patterns beyond documented hot-aggregate trade-offs in [[_memory/source/domain-model-specification|domain model specification]] §8
- Large-scale / high-concurrency on-sale handling ([[_memory/source/product-requirements|product requirements]] §6.2)
- Enterprise venue features, multi-currency, blockchain ticketing, paid secondary marketplace

Adding in-scope capabilities (e.g. a new bounded context) requires alignment with [[_memory/source/domain-model-specification|domain model specification]], [[_memory/source/feature-specification|feature specification]], and [[_memory/source/technical-design|technical design]] — not silent drift.

---

## X. Amendment process

To change an invariant in this document:

1. Propose the change with rationale (new `DEC-*` in [[_memory/source/product-requirements|product requirements]] or a dedicated spec in `docs/_memory/specs/`).
2. Update affected source memory ([[_memory/source/product-requirements|product requirements]], [[_memory/source/feature-specification|feature specification]], [[_memory/source/domain-model-specification|domain model specification]], [[_memory/source/technical-design|technical design]]) and Codex rules to match.
3. Amend this constitution in the same change set.

Silent drift — code or rules that contradict this document without amendment — is a defect.

---

## Document map

| Document | Role |
|----------|------|
| **This file** | Immutable principles |
| [[_memory/source/product-requirements|Product requirements]] | Product intent — why, who, scope, goals, decisions (`DEC-*`), guardrails (`QG-*`) |
| [[_memory/source/feature-specification|Feature specification]] | Observable capabilities — epics (`EP-*`), features (`F-*`), acceptance criteria, build order |
| [[_memory/source/domain-model-specification|Domain model specification]] | Domain model — bounded contexts, aggregates, invariants, events, context map |
| [[_memory/source/technical-design|Technical design]] | How it is built — architecture, CQRS, infrastructure, API, persistence, testing |
| [[_memory/specs/README|Implementation specs memory]] | Product specs (`/spec`) — committed |

**Reading order for agents:** constitution → product requirements → feature specification (for the feature at hand) → domain model specification (domain rules) → technical design (mechanics).
