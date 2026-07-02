---
title: Technical Architecture MOC
type: moc
status: active
tags:
  - moc/technical
  - architecture
---

# Technical Architecture MOC

Authoritative sources: [[CONSTITUTION|CONSTITUTION]] and [[_memory/source/technical-design|technical design]].

## Architecture

- Clean Architecture dependency direction: `Domain <- Application <- Infrastructure`, with `Api` as composition root.
- Domain stays pure C# with no EF Core, ASP.NET, MediatR, or Infrastructure dependencies.
- Commands and queries live in Application and flow through MediatR.
- Application owns ports; Infrastructure owns adapters.
- API endpoints are thin and return Contracts DTOs.

## Runtime topology

- .NET Aspire AppHost is the local topology source of truth.
- PostgreSQL is authoritative state.
- Redis is rebuildable cache only.
- MinIO stores binary assets.
- RabbitMQ carries integration events.
- SignalR handles realtime updates.
- Seq receives structured logs and OpenTelemetry traces.

## Persistence and API anchors

- Aggregate boundaries guide storage.
- Mutable aggregates use optimistic concurrency.
- Migrations are append-only.
- OpenAPI shape lives in `contracts/openapi/api.v1.yaml`.
- Do not hand-edit `web/src/generated/`.

## Testing anchors

- Domain unit tests for pure domain behavior.
- API integration tests for HTTP and infrastructure boundaries.
- Playwright e2e only when user-facing workflow risk justifies it.

## Related memory

- [[_memory/glossary/architecture-invariants]]
- [[domain-model]]
- [[feature-roadmap]]
