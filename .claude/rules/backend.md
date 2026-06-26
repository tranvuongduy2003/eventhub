---
description: ".NET 10 backend stack conventions — MediatR, FluentValidation, EF Core usage rules, and prohibited patterns (no Aspire client packages in service projects, no Task.Result, no raw SQL). Use when writing backend C# code alongside architecture.md."
paths:
  - "src/**/*.cs"
  - "src/**/*.csproj"
---

# BACKEND (.NET)

Source: [`docs/constitution.md`](../../docs/constitution.md), [`docs/technical.md`](../../docs/technical.md). Consult `CLAUDE.md` and **`architecture.md`** first. Tests → `backend-testing.md`.

Architecture, CQRS, DDD, and layer rules live in **`architecture.md`** — do not duplicate here.

## Stack

- **.NET 10**, nullable on, warnings as errors where project enables it
- **MediatR** + **FluentValidation** — see `architecture.md` §4
- **EF Core** — Infrastructure only; read queries `AsNoTracking` by default (Tech §6)

## Naming

- No `@` prefix on variable names for C# keywords — use descriptive names instead (e.g. `draftEvent` not `@event`, `domain` not `@event` for parameters)

## Types

- No **ValueTuples** `(T1, T2, …)` for method returns or parameters — define a `record` (or `sealed record`). Tuples lack named semantics at call sites, are not CLS-compliant for public API surfaces, and degrade readability. Use `PaginatedResult<T>` for paged queries; nest private helper records inside the class for internal use.

## DON'TS

- No **Aspire client/component packages** (`Aspire.*`, `CommunityToolkit.Aspire.*`) in `Api`, `Application`, `Infrastructure`, or `ServiceDefaults` — use native SDKs (EF Core/Npgsql, StackExchange.Redis, RabbitMQ.Client, Minio SDK, OpenTelemetry OTLP). Aspire hosting packages are **AppHost-only** (orchestration).
- No domain rules in Api endpoints — MediatR handlers only
- No `Task.Result` / `.Wait()` — async throughout
- No raw SQL strings — EF LINQ or parameterized APIs
- No reading `Environment.GetEnvironmentVariable` in handlers — `IOptions<T>` at startup
- No catching `Exception` without log + appropriate rethrow/result
- No **ValueTuples** `(T1, T2)` in method signatures — use `record` types (see **Types** section)
