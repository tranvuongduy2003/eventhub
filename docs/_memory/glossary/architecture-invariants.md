---
title: Architecture Invariants
type: invariant_index
status: active
tags:
  - architecture/invariants
  - constitution
---

# Architecture Invariants

Authoritative source: [[CONSTITUTION|CONSTITUTION]].

## Dependency direction

`Domain <- Application <- Infrastructure`, with `Api` as the composition root.

- Domain is pure C#.
- Application defines use cases and ports.
- Infrastructure implements adapters.
- Api dispatches through MediatR and maps to Contracts DTOs.

## CQRS

Commands mutate state and queries read state. Handlers live in Application and return `Result` or `Result<T>`.

Pipeline order is fixed in the Constitution; do not bypass it for writes that need transactions or domain events.

## Data

- PostgreSQL is authoritative.
- Redis is rebuildable cache only.
- MinIO stores binary assets.
- RabbitMQ carries cross-context integration events.
- Mutable aggregates use optimistic concurrency.

## API

- Endpoints are thin.
- Errors use RFC 7807 problem details.
- Success responses return resource DTOs directly.
- OpenAPI lives in `contracts/openapi/api.v1.yaml`.
- Generated frontend clients under `web/src/generated/` are protected output.

## Local topology

.NET Aspire AppHost is the local topology source of truth. Do not add hand-authored `docker-compose.yml` for local orchestration.

## Testing

Use meaningful, selective tests:

- Domain unit tests for aggregate and value object behavior.
- API integration tests for HTTP and infrastructure boundaries.
- Playwright e2e when a user-facing workflow requires it.

