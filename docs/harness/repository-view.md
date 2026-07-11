---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.repository-view
title: Repository View
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Repository View

Use this map to choose where a change belongs.

## Source

- `src/Domain`: aggregate behavior, value objects, domain events, and invariants.
- `src/Application`: use cases, validators, ports, result types, and MediatR pipeline behavior.
- `src/Infrastructure`: persistence, cache, session, object storage, hashing, repositories, and external adapters.
- `src/Api`: endpoints, request binding, authentication/session middleware, authorization, problem details, and OpenAPI.
- `src/AppHost`: Aspire orchestration for local development.
- `src/Contracts`: shared request/response contracts.
- `src/DataSeeder`: optional seed data and loading logic.
- `src/ServiceDefaults`: shared service defaults for the Aspire topology.

## Tests

- `tests/Domain.UnitTests`: domain behavior and validators.
- `tests/Api.IntegrationTests`: HTTP, auth/session, persistence, and integration behavior.
- `tests/Testing.Common`: shared test infrastructure.
- `e2e`: Playwright browser tests, page objects, fixtures, and helpers.

## Frontend

- `web/src/app`: providers, routes, route guards, and paths.
- `web/src/features`: feature screens, forms, API wrappers, and error mapping.
- `web/src/components/ui`: UI primitives.
- `web/src/lib`: API client, environment, and utilities.
- `web/src/store`: small client-side state stores.
- `web/src/generated`: generated API schema types.

## Contracts

- `contracts/openapi/api.v1.yaml`: committed OpenAPI contract.
- `scripts/openapi/sync-contract.mjs`: contract export and verification helper.
