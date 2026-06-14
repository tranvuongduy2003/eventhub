# Technical Design Document

**Project:** Clean Architecture + CQRS + DDD Boilerplate  
**Last Updated:** June 7, 2026

---

## 1. Architectural overview

### 1.1 Goals

- Keep business rules in a **pure Domain** layer with no framework dependencies.
- Separate **commands** (writes) from **queries** (reads) via CQRS and MediatR.
- Persist through **ports** (Application abstractions) implemented in Infrastructure.
- Run locally with **.NET Aspire** as the topology source of truth.

### 1.2 Styles

1. **Clean Architecture** — dependency rule: inner layers never reference outer layers.
2. **DDD (tactical)** — aggregates, value objects, domain events.
3. **CQRS** — distinct command/query handlers; shared PostgreSQL source of truth.

### 1.3 Logical view

```
┌─────────────────────────────────────┐
│           Api (ASP.NET Core)        │
│     REST endpoints + OpenAPI        │
└─────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│         Application (CQRS)          │
│   Handlers, validators, ports       │
└─────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│     Infrastructure (adapters)       │
│      EF Core, Redis, auth           │
└─────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│         Domain (pure C#)            │
└─────────────────────────────────────┘
```

---

## 2. Solution layout

```
src/
  AppHost/           Aspire orchestration
  ServiceDefaults/   Shared logging, health, service discovery
  Api/               HTTP host, endpoints, auth middleware
  Application/       Commands, queries, behaviors, ports
  Domain/            Aggregates, value objects, domain events
  Infrastructure/    EF Core, Redis, repository implementations
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

Handlers live in Application. Registration in `DependencyInjection.AddApplication`:

1. `DomainEventDispatchBehavior` — dispatch events after successful handler
2. `ValidationBehavior` — FluentValidation before handler
3. `LoggingBehavior`
4. `UnitOfWorkBehavior` — transaction + optimistic concurrency retry for commands
5. `PostCommitSessionCacheBehavior` — Redis session cache after commit

Commands implement `ICommand` / `ICommand<T>`; queries implement `IQuery<T>`. Handlers return `Result` or `Result<T>`.

### Sample handlers

| Type | Handler | Notes |
|------|---------|-------|
| Command | `RegisterUserCommand` | Creates user, session, domain event |
| Command | `LoginUserCommand` | Validates credentials, creates session |
| Command | `LogoutUserCommand` | Invalidates session |

---

## 5. Domain model (sample)

The template ships one bounded context to demonstrate DDD patterns:

| Aggregate | Responsibility |
|-----------|----------------|
| `User` | Registration invariants; raises `UserRegisteredEvent` |

Value objects: `Username`, `EmailAddress`, `Password`, `PasswordHash`, typed `UserId`.

Replace or extend this with your own aggregates. Keep domain logic inside aggregates — no anemic models.

---

## 6. Persistence

- **PostgreSQL** — authoritative state (`app` schema).
- **Redis** — optional rebuildable session cache (not source of truth).
- EF Core: `NoTracking` by default; configurations in Infrastructure.
- Migrations apply on startup in Development.

### 6.1 Design principles

1. **Aggregate-aligned storage** — one primary table per aggregate root.
2. **PostgreSQL is authoritative** — Redis holds rebuildable cache only.
3. **Declarative constraints** — enforce invariants at the database where possible.
4. **Optimistic concurrency** — `row_version` on mutable aggregates.
5. **UTC timestamps** — store as `TIMESTAMPTZ`.

### 6.2 Schema (`app`)

```
users 1:N user_sessions
```

**`app.users`**

| Column | Type | Notes |
|--------|------|-------|
| `id` | `uuid` | PK |
| `username` | `varchar(32)` | unique |
| `email` | `varchar(254)` | unique, normalized |
| `password_hash` | `varchar(255)` | required |
| `created_at` | `timestamptz` | |
| `updated_at` | `timestamptz` | |
| `row_version` | `bigint` | concurrency token |

Indexes: `ux_users_username`, `ux_users_email`.

**`app.user_sessions`**

| Column | Type | Notes |
|--------|------|-------|
| `id` | `uuid` | PK (session id) |
| `user_id` | `uuid` | FK → users |
| `expires_at` | `timestamptz` | |
| `created_at` | `timestamptz` | |

Index on `user_id` for lookup; expired sessions cleaned by application policy.

### 6.3 Redis keys (session cache)

| Key pattern | Purpose | Rebuildable |
|-------------|---------|-------------|
| `session:{sessionId}` | Active session payload | Yes — from `user_sessions` |

Session cache is written **after** PostgreSQL commit (see `PostCommitSessionCacheBehavior`).

### 6.4 Migrations

- Location: `src/Infrastructure/Migrations/`
- Generate:

```bash
dotnet ef migrations add <Name> \
  -p src/Infrastructure/Solution.Infrastructure.csproj \
  -s src/Api/Solution.Api.csproj
```

- **Never edit** merged migrations — add a new migration instead.
- Development: applied on Api startup.

### 6.5 Transaction boundaries

| Operation | Boundary |
|-----------|----------|
| Register user | Insert user + session in one unit of work; cache session after commit |
| Login | Validate user; insert/replace session; cache after commit |
| Logout | Delete/invalidate session; remove cache entry |

---

## 7. API conventions

- Minimal endpoints implementing `IEndpoint`; discovered via assembly scan.
- RFC 7807 problem details for errors (`ApiProblemDetails`).
- Two-layer validation: JSON binding (400) vs FluentValidation (422).
- Cookie-based session auth for browser clients; `ICurrentUserAccessor` in handlers.

### Routes (sample)

| Method | Path | Handler |
|--------|------|---------|
| POST | `/api/users` | RegisterUser |
| POST | `/api/auth/login` | LoginUser |
| POST | `/api/auth/logout` | LogoutUser |
| GET | `/health` | Health check |

---

## 8. Configuration

Layering (later wins): `appsettings.json` → `appsettings.Development.json` → Aspire env → user secrets.

| Section | Purpose |
|---------|---------|
| `Session` | Cookie name, expiration |
| `Concurrency` | Unit-of-work retry for optimistic concurrency |

Connection strings (Aspire-injected): `ConnectionStrings__app`, `ConnectionStrings__cache`.

---

## 9. Local development

1. Docker Desktop running
2. `dotnet run --project src/AppHost/Solution.AppHost.csproj`
3. Aspire dashboard for logs and health
4. API Scalar UI at `/scalar` (Development)

---

## 10. Testing

| Project | Focus |
|---------|-------|
| `Domain.UnitTests` | Aggregates and value objects (pure, no DI) |
| `Api.IntegrationTests` | HTTP + Testcontainers PostgreSQL/Redis |

Integration tests use fakes at Application **ports**, not domain mocks.

---

## 11. OpenAPI contract

REST shapes are maintained in [`contracts/openapi/api.v1.yaml`](../contracts/openapi/api.v1.yaml). Export from the API build via scripts in `contracts/openapi/README.md`.
