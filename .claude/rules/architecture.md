---
description: "Clean Architecture, CQRS, DDD domain rules, and layer conventions for EventHub backend. Use when writing or editing C# code in src/ or tests/ — covers dependency direction, aggregate design, command/query handlers, FluentValidation pipeline, ports/adapters, persistence alignment, and naming. Replaces procedural skills for layer boundaries and domain modeling."
paths:
  - "src/**/*.cs"
  - "src/**/*.csproj"
  - "tests/**/*.cs"
---

# ARCHITECTURE — Clean Architecture · CQRS · DDD

Source: [`docs/constitution.md`](../../docs/constitution.md) I–II · [`docs/technical.md`](../../docs/technical.md) §1–6 · [`docs/ddd.md`](../../docs/ddd.md). Consult `CLAUDE.md` first. HTTP → `api-guidelines.md` · EF migrations → `migration.md` · tests → `backend-testing.md`.

This rule replaces procedural skills for layer boundaries, handlers, and domain modeling. **Do not load separate architecture/CQRS/domain skills** when this rule applies.

---

## 1. Dependency rule

```
Domain ← Application ← Infrastructure
              ↑
             Api (composition root)
```

| Layer | May reference | Must never reference |
|-------|---------------|----------------------|
| **Domain** | — | EF Core, ASP.NET, MediatR, Infrastructure, Contracts |
| **Application** | Domain, Contracts | Infrastructure, HTTP types |
| **Infrastructure** | Application, Domain | Api |
| **Api** | Application, Infrastructure, Contracts | Domain logic in endpoints |

- **Api** sends MediatR requests only — no business rules in endpoints.
- **Handlers** depend on **ports** (`IUserRepository`, `IUnitOfWork`, …), never on EF/Redis/RabbitMQ/MinIO types.
- **Authorization and ownership** checks live in Application handlers (`ICurrentUserAccessor`), not in Api alone.

---

## 2. Repository layout

```
src/
  AppHost/           Aspire orchestration
  ServiceDefaults/   Logging, telemetry, health
  Api/               IEndpoint, SignalR hubs, auth middleware
  Application/       CQRS, validators, behaviors, ports
  Domain/            Aggregates, value objects, domain events (pure C#)
  Infrastructure/    EF Core, Redis, MinIO, RabbitMQ adapters
  Contracts/         HTTP request/response DTOs
```

Every project under `src/` and `tests/` includes **`AssemblyReference.cs`**. Register MediatR, FluentValidation, EF configurations, and endpoint discovery via `AssemblyReference.Assembly`.

### Application folder convention

Organize by **feature / bounded context**, then by command or query:

```
Application/
  Abstractions/          Ports (Persistence, Auth, Messaging, …)
  Behaviors/               MediatR pipeline
  Common/                  Result, Error
  <Feature>/
    Commands/
      PlaceOrderCommand.cs
      PlaceOrderCommandHandler.cs
      PlaceOrderCommandValidator.cs
    Queries/
      GetEventQuery.cs
      GetEventQueryHandler.cs
    EventHandlers/         IDomainEventHandler<T> (in-process)
```

- One command + handler + validator per write use case.
- One query + handler per read use case.
- Shared errors as static classes (e.g. `RegistrationErrors.UsernameTaken`).

---

## 3. Domain layer (DDD tactical)

**Authoritative model:** [`docs/ddd.md`](../../docs/ddd.md) — bounded contexts, aggregates, `INV-*`, events.

### Base types (`Domain/Abstractions/`)

| Type | Role |
|------|------|
| `Entity<TId>` | Identity equality |
| `AggregateRoot<TId>` | Consistency boundary; collects domain events |
| `ValueObject` | Immutable; equality by components |
| `DomainEvent` | Past-tense fact; `OccurredOn` UTC |
| `DomainException` / `BusinessRuleValidationException` | Rejected invariant with stable `code` |

### Aggregate rules

- **Behavior on the aggregate** — factories (`Register`, `Place`, `Publish`), state transitions, invariant enforcement.
- **No anemic models** — handlers orchestrate; they do not encode business rules that belong on the aggregate.
- **Private parameterless constructor** for EF persistence; use `FromPersistence` or factory for rehydration.
- **Raise events inside the aggregate** via `Raise(...)`; clear after collection in the handler.
- **Reference other aggregates by typed id only** (`EventId`, `OrderId`) — never hold another aggregate instance.
- One **repository port per aggregate root** in Application.

### Value object rules

- **Validate in `Create(...)`** — invalid input throws `BusinessRuleValidationException` with a stable code (`EMAIL_INVALID`, …).
- **Immutable** after construction; expose data via properties, not public setters.
- **No primitives** for domain identity — use typed ids (`UserId`, `EventId`, …).
- Subclass `ValueObject` and implement `GetEqualityComponents()`.

### Domain events

| Scope | Raised by | Handled |
|-------|-----------|---------|
| **In-process (domain)** | Aggregate | `IDomainEventHandler<T>` in Application, same unit of work |
| **Integration** | Application/Infrastructure after commit | RabbitMQ consumers; idempotent |

Collect events in the handler:

```csharp
pendingDomainEventsCollector.AddRange(aggregate.DomainEvents);
aggregate.ClearDomainEvents();
```

Do not dispatch domain events manually from handlers — `DomainEventDispatchBehavior` runs after a successful `Result`.

### Domain services

Use only when logic does not belong to one entity (e.g. `SVC-DiscountPolicy` in `ddd.md`). **Cross-aggregate orchestration** is Application responsibility, not Domain.

### Modular monolith

Bounded contexts are **logical modules** inside `EventHub.Domain` (see `ddd.md` §2). Strong consistency inside an aggregate; cross-context via integration events unless explicitly documented otherwise (e.g. `PlaceOrder` + `Event.Reserve` in one transaction per `ddd.md` §8).

---

## 4. CQRS and MediatR

### Commands (writes)

```csharp
public sealed record RegisterUserCommand(string Username, string Email, string Password)
    : ICommand<RegisterUserResult>;
```

- Implement `ICommand` or `ICommand<TResponse>`.
- **`ICommand` extends `IUnitOfWorkRequest`** — `UnitOfWorkBehavior` wraps commands in a transaction with optimistic-concurrency retry.
- Handler inherits `CommandHandler<TCommand>` or `CommandHandler<TCommand, TResponse>`.
- Return **`Result`** or **`Result<T>`** — never throw for expected business failures.
- Map `BusinessRuleValidationException` to `Error.Validation(...)` in the handler catch block.

### Queries (reads)

```csharp
public sealed record GetCurrentUserQuery : IQuery<CurrentUserResult>;
```

- Implement `IQuery<TResponse>`; handler inherits `QueryHandler<TQuery, TResponse>`.
- **No unit-of-work** — queries skip `UnitOfWorkBehavior` (not `IUnitOfWorkRequest`).
- Return **Contracts DTOs** or Application result records mapped to Contracts at the Api layer — **never domain entities** on the read path.
- Use `AsNoTracking` in Infrastructure for read queries.

### FluentValidation

- Every command has a **`{Command}Validator`** in the same folder.
- Validation runs in **`ValidationBehavior`** before the handler — failures never reach the handler.
- Api JSON binding failures → `400`; FluentValidation / domain rejection → `422` (see `api-guidelines.md`).

### Pipeline order (fixed — do not reorder)

Registered in `Application/DependencyInjection.cs`:

1. **`DomainEventDispatchBehavior`** — after successful handler; drains `IPendingDomainEventsCollector`
2. **`ValidationBehavior`** — FluentValidation
3. **`LoggingBehavior`**
4. **`UnitOfWorkBehavior`** — transaction + concurrency retry for `IUnitOfWorkRequest`
5. **`PostCommitSessionCacheBehavior`** — Redis session cache **after** PostgreSQL commit

Do not bypass the pipeline for writes that need transactions, domain events, or post-commit cache.

---

## 5. Handler patterns

### Command handler (orchestration)

1. Load aggregates via repository ports (or create via factory).
2. Call aggregate behavior; catch `BusinessRuleValidationException` → `Result` failure.
3. Persist via repository; enqueue session cache / integration messages if needed.
4. Collect domain events → `IPendingDomainEventsCollector`; clear aggregate events.
5. Return `Result` success with response DTO/record.

**Uniqueness / existence checks** that need the database may live in the handler; **invariant enforcement** stays on the aggregate or value object.

### Query handler

1. Resolve caller (`ICurrentUserAccessor`) and authorize.
2. Read via repository or read model port.
3. Map to result type; return `Error.NotFound` / `Error.Unauthorized` as appropriate.

### Error model

Use `Application/Common/Error.cs` — stable `code` strings consumed by Api as RFC 7807 problem details. Prefer typed static error fields over ad-hoc strings.

---

## 6. Ports and Infrastructure

| Port (Application) | Adapter (Infrastructure) |
|--------------------|---------------------------|
| `IUserRepository`, `IUnitOfWork` | EF Core repositories, `UnitOfWork` |
| `ISessionStore`, `ICacheService` | Redis |
| Storage port | MinIO |
| Messaging publisher | RabbitMQ |
| `IPaymentGateway`, `IEmailSender` | External provider adapters |

- **PostgreSQL** (`app` schema) is authoritative.
- **Redis** — rebuildable cache only; written after commit.
- **MinIO** — binary assets; DB stores key/URL only.
- **Optimistic concurrency** — `row_version` on mutable roots; retry in `UnitOfWorkBehavior` (see `ConcurrencyOptions`).

---

## 7. Persistence alignment

- One primary table per aggregate root; FKs reflect ownership, not cross-aggregate object graphs.
- UTC timestamps (`timestamptz`); declarative DB constraints where they reinforce invariants.
- EF configurations live in **Infrastructure only**; Domain has no ORM attributes.
- Migrations: append-only in `Infrastructure/Migrations/` — see `migration.md`.

---

## 8. Naming (Constitution VI)

- No `/// <summary>` XML doc comments.
- No abbreviations in type, method, property, file, or parameter names (except framework terms like `DbContext`, `Guid`).
- Namespace prefix: **`EventHub.*`**.
- No **ValueTuples** `(T1, T2, …)` for method returns or parameters — define a `record` or `sealed record` instead. Use `PaginatedResult<T>` (in `Application/Common/`) for paged repository results; nest private helper records inside the class for internal use.

---

## 9. Checklist (new feature)

- [ ] Aggregate / VO behavior in **Domain** aligns with `ddd.md` invariants
- [ ] Command + validator + handler (+ query if read side) in **Application**
- [ ] Repository / port in **Application**; implementation in **Infrastructure**
- [ ] Endpoint maps to MediatR; response uses **Contracts** DTO
- [ ] Commands that mutate state implement **`ICommand`** (unit of work)
- [ ] Domain events collected and cleared; integration events idempotent
- [ ] Tests: domain unit tests pure; integration tests at ports (see `backend-testing.md`)

---

## DON'TS

- No MediatR, EF, or HTTP types in **Domain**
- No domain rules or transaction logic in **Api** endpoints
- No Infrastructure types in **Application** handlers
- No returning domain entities from query handlers or HTTP responses
- No bypassing **`UnitOfWorkBehavior`** for multi-step writes that must be atomic
- No Redis/MinIO as sole source of truth
- No `Task.Result` / `.Wait()` — async throughout
- No raw SQL strings in handlers — EF LINQ or parameterized APIs in Infrastructure
