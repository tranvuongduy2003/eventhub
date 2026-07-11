# EventHub Infrastructure instructions

## Scope

Applies to `src/Infrastructure/**`. Inherits `src/AGENTS.md` and the root instructions.

## Adapter boundaries

- Implement Application ports for EF Core, repositories, cache/session, object storage, messaging, payment, email, and other external systems.
- Infrastructure may reference Application and Domain; it must not reference Api.
- PostgreSQL schema `app` is authoritative. Redis and read projections must remain disposable/rebuildable.
- Default EF queries to no tracking unless mutation or identity resolution requires tracking.
- Use optimistic concurrency for mutable aggregates and bounded retries only at the documented unit-of-work boundary.
- Keep relational records for binary assets to object keys and metadata; do not store bytes or expiring presigned URLs.

## Messaging and external systems

- Store integration events transactionally with state changes; publish only after commit.
- Consumers must use stable message identifiers plus inbox/deduplication so redelivery is safe.
- Preserve observable retry or reconstruction context; do not silently discard failures.
- Payment adapters form an anti-corruption layer, validate provider signatures, store no card data, and never make EventHub the holder of funds.
- Secrets and provider credentials must never be committed or placed in domain entities.

## EF Core migrations

- Existing shared migrations are immutable.
- Generate new migrations with EF tooling; do not hand-author a replacement for a generated migration.
- Review every generated migration before keeping it.
- Make required fields, max lengths, indexes, foreign keys, delete behavior, defaults, concurrency tokens, and schema names explicit.
- Destructive or data-shaping changes require a new migration and an explicit data-migration/rollback plan.
- Update integration tests when persistence behavior changes.
- Never edit migration designer/snapshot output merely to make a diff look cleaner.

## Verification

For persistence or adapter changes, run focused integration tests against real engines through the existing Testcontainers/Aspire-supported fixtures when a fake would hide material behavior, then run:

```powershell
dotnet build EventHub.slnx -c Release
dotnet test EventHub.slnx -c Release
```
