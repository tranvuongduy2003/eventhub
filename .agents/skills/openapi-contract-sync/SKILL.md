---
name: openapi-contract-sync
description: Keeps the REST OpenAPI contract and frontend TypeScript types in sync with EventHub.Api. Use when adding or changing HTTP endpoints, updating contracts/openapi/api.v1.yaml, running api:export/api:verify/api:codegen, fixing CI OpenAPI drift, or wiring type-safe API clients from generated paths/components.
---

# OpenAPI contract sync (REST)

HTTP contract flow for **EventHub**.

## Source of truth

| Artifact | Git | Role |
|----------|-----|------|
| `contracts/openapi/api.v1.yaml` | **Committed** | Reviewable REST contract |
| `contracts/openapi/.build/api-v1.json` | Ignored | MSBuild export (JSON only at build time) |
| `web/src/generated/api-schema.ts` | Ignored | `openapi-typescript` output |

## Pipeline

```
EventHub.Api  â†’  dotnet build (OpenApiGenerateDocuments=true)
                      â†’  scripts/openapi/sync-contract.mjs  â†’  api.v1.yaml
                      â†’  openapi-typescript  â†’  web/src/generated/api-schema.ts
                      â†’  web app imports via web/src/lib/api/openapi.ts
```

**Stack:** `Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server` (Api csproj) â†’ `openapi-typescript` (web).

## When you change REST endpoints

Copy and track:

```
- [ ] Endpoint has OpenAPI metadata (.WithName, .WithTags, Produces/Accepts as needed)
- [ ] yarn --cwd web api:export
- [ ] yarn --cwd web api:codegen (or rely on predev/prebuild)
- [ ] Frontend uses paths/components from @/lib/api (or @/generated/api-schema)
- [ ] Commit only contracts/openapi/api.v1.yaml (not web/src/generated/)
- [ ] yarn --cwd web api:verify passes locally
```

### Commands (repo root)

```bash
yarn --cwd web api:export    # rebuild API + refresh committed YAML
yarn --cwd web api:codegen   # YAML â†’ TypeScript
yarn --cwd web api:verify    # CI check: YAML matches API (no write)
```

**PowerShell:** `.\scripts\Export-OpenApi.ps1` (same as `api:export`).

`predev` and `prebuild` run `api:codegen` automatically.

## Frontend usage

Import generated types from the barrel:

```typescript
import type { paths, components, ApiPath, ApiJsonBody, ApiJsonResponse } from '@/lib/api'

// Example when schemas exist:
// type Body = components['schemas']['LoginRequest']
// type Ok = ApiJsonResponse<'/api/wallet', 'get'>
```

Do not hand-edit `web/src/generated/api-schema.ts`.

## Backend notes

- `src/Api/Program.cs`: `MapOpenApi()` is always registered; Scalar stays Development-only.
- Export MSBuild props live in `src/Api/EventHub.Api.csproj`:
  - `OpenApiGenerateDocuments=false` by default (export only when requested).
  - Build flag: `-p:OpenApiGenerateDocuments=true`
  - Output file name must be alphanumeric/`_`/`-` only â†’ **`api-v1`** (not `api.v1`).
- Map DTOs from `EventHub.Contracts`; never expose domain types in OpenAPI.

## CI

[`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) runs `yarn --cwd web api:verify` after .NET build. If it fails, run `api:export` and commit the YAML.

Include `api:verify` in local PR checks when touching Api or `contracts/openapi/` (see `create-pr` skill).

## Out of scope

- SignalR hub messages â€” keep `web/src/types/realtime.ts` aligned with `src/Contracts/Realtime/` manually.
- No `contracts/signalr/` codegen path.

## DON'Ts

- Do not commit `web/src/generated/` or `contracts/openapi/.build/`.
- Do not edit `api.v1.yaml` by hand without re-running `api:export` afterward.
- Do not add dots to MSBuild `--file-name` (build fails).
- Do not block OpenAPI export on a running database (build-time generation uses `GetDocument`, not HTTP).

## More detail

Repo README section **API contract (OpenAPI â†’ TypeScript)** and [`contracts/openapi/README.md`](../../../contracts/openapi/README.md) if present.
