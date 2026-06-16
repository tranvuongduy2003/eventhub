# BACKEND (.NET)

Source: [`docs/constitution.md`](docs/constitution.md), [`docs/technical.md`](docs/technical.md). Consult `core.md` and **`architecture.md`** first. Tests → `backend-testing.md`.

Architecture, CQRS, DDD, and layer rules live in **`architecture.md`** — do not duplicate here.

## Stack

- **.NET 10**, nullable on, warnings as errors where project enables it
- **MediatR** + **FluentValidation** — see `architecture.md` §4
- **EF Core** — Infrastructure only; read queries `AsNoTracking` by default (Tech §6)

## DON'TS

- ❌ No **Aspire client/component packages** (`Aspire.*`, `CommunityToolkit.Aspire.*`) in `Api`, `Application`, `Infrastructure`, or `ServiceDefaults` — use native SDKs (EF Core/Npgsql, StackExchange.Redis, RabbitMQ.Client, Minio SDK, OpenTelemetry OTLP). Aspire hosting packages are **AppHost-only** (orchestration).
- ❌ No domain rules in Api endpoints — MediatR handlers only
- ❌ No `Task.Result` / `.Wait()` — async throughout
- ❌ No raw SQL strings — EF LINQ or parameterized APIs
- ❌ No reading `Environment.GetEnvironmentVariable` in handlers — `IOptions<T>` at startup
- ❌ No catching `Exception` without log + appropriate rethrow/result
