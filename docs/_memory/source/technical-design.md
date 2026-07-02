# Technical Design Document

**Project:** EventHub
**Built on:** Clean Architecture + CQRS + DDD (.NET / .NET Aspire)
**Related documents:** Product intent → [`product-requirements.md`](product-requirements.md) · Feature specifications → [`feature-specification.md`](feature-specification.md)
**Last Updated:** June 14, 2026

> **Scope of this document.** This is `technical-design.md`: it describes **how EventHub is built** — architecture, patterns, infrastructure, and conventions. It is purely technical; business behavior and domain rules belong to `feature-specification.md`.

---

## 1. Architectural overview

### 1.1 Goals

- Keep business rules in a **pure Domain** layer with no framework dependencies.
- Separate **commands** (writes) from **queries** (reads) via CQRS and MediatR.
- Persist and integrate through **ports** (Application abstractions) implemented as **adapters** in Infrastructure.
- Guarantee **write consistency under concurrent access** via optimistic concurrency (no distributed locks).
- Run locally with **.NET Aspire** as the topology source of truth, with every backing service provisioned and discoverable.

### 1.2 Styles

1. **Clean Architecture** — dependency rule: inner layers never reference outer layers.
2. **DDD (tactical)** — the Domain layer is structured with aggregates, value objects, and domain events. (Concrete models are not described here; they belong to the feature docs.)
3. **CQRS** — distinct command/query handlers; shared PostgreSQL source of truth.
4. **Ports & adapters** — external systems (cache, storage, messaging, realtime) sit behind Application ports and are implemented in Infrastructure.

### 1.3 Logical view

Requests flow top-down through the layers — **Api** (REST endpoints, OpenAPI, SignalR hubs) → **Application** (CQRS handlers, validators, ports) → **Infrastructure** (EF Core, Redis, MinIO, RabbitMQ adapters) → **Domain** (pure C#) — while the dependency rule points the opposite way: only outer layers reference inner ones (see §3). Logging, telemetry, and health checks are cross-cutting via `ServiceDefaults` (OpenTelemetry → Seq).

---

## 2. Repository layout

```
src/
  AppHost/           Aspire orchestration (EventHub.AppHost.csproj)
  ServiceDefaults/   Shared logging, telemetry, health, service discovery
  Api/               HTTP host, endpoints, SignalR hubs, auth middleware
  Application/       Commands, queries, behaviors, ports
  Domain/            Aggregates, value objects, domain events (pure C#)
  Infrastructure/    EF Core, Redis, MinIO, RabbitMQ adapters, repositories
  Contracts/         HTTP request/response DTOs
tests/
  Domain.UnitTests/
  Api.IntegrationTests/
  Testing.Common/
```

---

## 3. Layer rules

| Layer | References | Must not reference |
|-------|------------|-------------------|
| Domain | — | EF, ASP.NET, MediatR, Infrastructure |
| Application | Domain, Contracts | Infrastructure, HTTP |
| Infrastructure | Application, Domain | Api |
| Api | Application, Infrastructure, Contracts | Domain logic in endpoints |

Every project includes `AssemblyReference.cs`. Use `AssemblyReference.Assembly` for MediatR, FluentValidation, EF configuration, and endpoint discovery.

---

## 4. CQRS and MediatR pipeline

Handlers live in Application. Commands implement `ICommand` / `ICommand<T>`; queries implement `IQuery<T>`. Handlers return `Result` or `Result<T>`.

Pipeline behaviors, registered in `DependencyInjection.AddApplication` (order matters):

1. `DomainEventDispatchBehavior` — dispatch **in-process** domain events after a successful handler
2. `ValidationBehavior` — FluentValidation before the handler
3. `LoggingBehavior`
4. `UnitOfWorkBehavior` — transaction boundary + optimistic-concurrency retry for commands
5. `PostCommitSessionCacheBehavior` — write-through to the Redis cache after commit

**In-process vs out-of-process events.** Domain events are handled synchronously inside the request's unit of work. Side effects that are slow, external, or better decoupled are emitted as **integration messages onto RabbitMQ** and processed asynchronously by consumers (see §5).

---

## 5. Infrastructure & runtime components

External systems are provisioned by Aspire and accessed through Application ports (except hosting-level concerns such as SignalR and telemetry).

| Component | Technology | Role | Where it lives |
|-----------|------------|------|----------------|
| Relational store | **PostgreSQL** | Authoritative state (`app` schema) | Infrastructure (EF Core) |
| Cache | **Redis** | Response/session caching and rebuildable derived data; optional SignalR backplane | Infrastructure adapter behind a cache port |
| Object storage | **MinIO** (S3-compatible) | Binary assets such as image uploads; PostgreSQL stores only the object key/URL, never the bytes | Infrastructure adapter behind a storage port |
| Messaging | **RabbitMQ** | Asynchronous/background work and integration events; decouples slow or external side effects from the request path | Infrastructure (publisher + consumers) |
| Realtime | **SignalR** | Server→client push over WebSockets via hubs | Api host (Redis backplane when scaled out) |
| Logging & telemetry | **Seq** | Sink for structured logs (and traces/metrics) emitted via ServiceDefaults / OpenTelemetry | Cross-cutting |
| Orchestration | **.NET Aspire** | Local topology, service discovery, connection-string/env injection | AppHost |

**Notes**
- **Aspire AppHost** provisions containers and injects connection strings / env (e.g. `OTEL_EXPORTER_OTLP_ENDPOINT` for Seq). **Service projects** (`Api`, `Application`, `Infrastructure`, `ServiceDefaults`) use **native SDKs only** — no `Aspire.*` or `CommunityToolkit.Aspire.*` client packages (EF Core/Npgsql, StackExchange.Redis, RabbitMQ.Client, Minio SDK, OpenTelemetry OTLP).
- **Redis** is never a source of truth; anything cached must be rebuildable from PostgreSQL.
- **MinIO** keeps large binaries out of the relational database; uploads go through the storage port and only references are persisted.
- **RabbitMQ** carries integration messages between background consumers and the request path; consumers are idempotent so redelivered messages are safe.
- **SignalR** hubs are hosted alongside REST endpoints; a Redis backplane coordinates connections if more than one instance runs.

---

## 6. Persistence

- **PostgreSQL** — authoritative state (`app` schema).
- **Redis** — optional, rebuildable cache (not source of truth).
- **MinIO** — object storage for binary/large assets; the relational schema holds only keys/URLs.
- EF Core: `NoTracking` by default; configurations in Infrastructure.
- **Optimistic concurrency:** mutable aggregates carry a row version; the `UnitOfWorkBehavior` retry (configured under `Concurrency`) resolves concurrent-write races without locks.
- Migrations apply on startup in Development.

Schema, tables, and indexes are defined by Infrastructure EF Core configurations and append-only migrations until a dedicated database source note exists.

---

## 7. API conventions

- Minimal endpoints implementing `IEndpoint`; discovered via assembly scan.
- RFC 7807 problem details for errors (`ApiProblemDetails`).
- Two-layer validation: JSON binding (400) vs FluentValidation (422).
- Cookie-based session auth for browser clients; `ICurrentUserAccessor` in handlers.
- Realtime updates are delivered through **SignalR hubs** hosted next to the REST endpoints; clients authenticate with the same session.

---

## 8. Configuration

Layering (later wins): `appsettings.json` → `appsettings.Development.json` → Aspire env → user secrets.

| Section | Purpose |
|---------|---------|
| `Session` | Cookie name, expiration |
| `Concurrency` | Unit-of-work retry for optimistic concurrency |
| `Cache` | Redis usage and TTLs |
| `Storage` | MinIO endpoint and bucket (credentials kept in user secrets) |
| `Messaging` | RabbitMQ exchanges/queues and consumer settings |
| `Realtime` | SignalR hub options and backplane |
| `Logging` | Seq endpoint and minimum levels |

Connection strings / endpoints are **Aspire-injected** and named after the AppHost resources, e.g. `ConnectionStrings__app` (PostgreSQL), `ConnectionStrings__cache` (Redis), plus the RabbitMQ, MinIO, and Seq resources. Secrets are never committed.

---

## 9. Observability & logging

- **Structured logging** plus **OpenTelemetry** traces and metrics are configured once in `ServiceDefaults` and exported to **Seq** via the **OTLP exporter** (`OTEL_EXPORTER_OTLP_ENDPOINT` from AppHost when Seq is referenced).
- **Health checks** are exposed at `/health` and surfaced in the Aspire dashboard.
- Correlation/trace IDs flow through the MediatR `LoggingBehavior` so a request can be followed end to end in Seq.

---

## 10. Local development

1. Docker Desktop running
2. `dotnet run --project src/AppHost/EventHub.AppHost.csproj`
3. Aspire provisions PostgreSQL, Redis, MinIO, RabbitMQ, and Seq as containers and wires connection strings automatically
4. Dashboards: **Aspire** (logs/health/topology), **Seq** (logs/traces), **MinIO console** (objects), **RabbitMQ management UI** (queues)
5. API Scalar UI at `/scalar` (Development)

---

## 11. Testing

| Project | Focus |
|---------|-------|
| `Domain.UnitTests` | Domain layer logic (pure, no DI) |
| `Api.IntegrationTests` | HTTP + Testcontainers for PostgreSQL, Redis, RabbitMQ, and MinIO |

Integration tests use **fakes at Application ports**, not domain mocks. External services with no in-process fake are exercised through Testcontainers so the adapters are covered against real engines.

---

## 12. OpenAPI contract

REST shapes are maintained in [`contracts/openapi/api.v1.yaml`](../contracts/openapi/api.v1.yaml). Export from the API build via scripts in `contracts/openapi/README.md`.
