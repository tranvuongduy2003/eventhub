# EventHub backend instructions

## Scope

Applies to all code under `src/**`. Read the nearest child `AGENTS.md` before editing a specialized layer.

## Required context

- Read `technical.md` for architecture, domain, consistency, persistence, integration, API, security, and verification rules.
- Read the relevant `F-*` records in `features.md` before changing observable behavior.
- Read `product.md` before changing scope, pricing, fairness, payment/data handling, mobile experience, or another `DEC-*`/`QG-*` concern.
- Do not infer behavior from delivery status alone. Acceptance criteria and applicable `INV-*` rules are the contract.

## Backend boundaries

- Preserve `Domain <- Application <- Infrastructure`; `Api` is the composition root.
- Keep the system a modular monolith and one deployable API host. Do not introduce a microservice boundary without revising `technical.md`.
- Keep business invariants in Domain, use-case orchestration and ports in Application, adapters in Infrastructure, HTTP/auth transport in Api, and request/response shapes in Contracts.
- Use typed identities across aggregate and bounded-context boundaries; do not pass aggregate object graphs between contexts.
- Use `Result`/`Error` for expected failures. Reserve exceptions for unexpected failures.
- Inject `IClock`; do not read system time directly in domain or application logic.
- Keep sensitive values out of logs, errors, comments, fixtures, and telemetry.
- Enforce protected operations on the backend. Frontend guards are not authorization.

## Correctness priorities

Treat these as high risk whenever affected:

- no oversell and reservation correctness (`INV-10`, F-3.4, F-5.3, F-5.5);
- snapshotted and transparent prices (`INV-20`, `INV-25`, QG-2);
- payment/webhook idempotency (`INV-30` through `INV-33`);
- exactly-once ticket issuance and admission (`INV-40` through `INV-44`);
- event-scoped role enforcement, especially Staff versus Owner operations;
- cancellation, refund, notification, and projection behavior under message redelivery.

## Local runtime

- `.NET Aspire` AppHost is the local topology source of truth.
- Do not add hand-authored Docker Compose.
- PostgreSQL is authoritative; Redis and read projections are rebuildable.
- MinIO stores binary assets by object key and metadata, never expiring presigned URLs in relational state.
- Channel-based consumers and external callbacks must be retry-safe and idempotent.
- Configure shared telemetry, service discovery, resilience, and health through `ServiceDefaults` rather than duplicating setup.

## Verification

Run the narrowest affected checks first, then broaden when the change crosses layers:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
dotnet build EventHub.slnx -c Release
dotnet test EventHub.slnx -c Release
```

A feature is complete only when relevant acceptance criteria are represented in code and the highest-risk criteria have automated evidence.
