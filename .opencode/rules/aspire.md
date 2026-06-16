# .NET ASPIRE

Source: [`docs/constitution.md`](docs/constitution.md) V, [`docs/technical.md`](docs/technical.md) §8–10. Consult `core.md` first.

**No aspire skill folder** — topology and workflow are in Constitution V and Tech §8–10.

## MCP (local debugging only)

| Server | When |
|--------|------|
| **aspire** (`.mcp.json`) | Running stack: resources, logs, traces |
| **postgres** (`.mcp.json`) | Read-only SQL against local `app` database · see `postgres-mcp` skill |
| **neo4j-graphrag** (`.mcp.json`) | Graph queries, vector/fulltext search · see `neo4j-graphrag` skill |

## Topology (source of truth = AppHost)

| Resource | Role |
|----------|------|
| PostgreSQL + volume | Source of truth |
| Redis | Session / response cache |
| MinIO | Object storage (cover images, avatars) |
| RabbitMQ | Integration events |
| Seq | Logs / traces |
| Api | REST + OpenAPI + SignalR |
| `web/` (Yarn + Vite) | Frontend — `AddViteApp("web", ...).WithYarn(...)` |

**No hand-authored docker-compose** for orchestration.

## Wiring

- Connection strings → Api (`ConnectionStrings__app`, `ConnectionStrings__cache`, etc.)
- API URL → `VITE_*` for frontend
- CORS → `https://localhost:5000` for Vite

## ServiceDefaults

Api calls ServiceDefaults extensions once — logging, health checks, service discovery, **OpenTelemetry OTLP** (Seq via `OTEL_EXPORTER_OTLP_ENDPOINT` injected by AppHost). **No `Aspire.*` client packages** in service projects — see `backend.md`.

## Client libraries (service projects)

| Concern | Use (native) | Not in Api/Application/Infrastructure |
|---------|----------------|--------------------------------------|
| PostgreSQL | `Npgsql` / EF Core | `Aspire.Npgsql.*` |
| Redis | `StackExchange.Redis` | `Aspire.StackExchange.Redis` |
| RabbitMQ | `RabbitMQ.Client` | `Aspire.RabbitMQ.Client` |
| MinIO | `Minio` SDK | `CommunityToolkit.Aspire.Minio.*` |
| Seq / telemetry | OpenTelemetry OTLP exporter | `Aspire.Seq` |

Aspire **hosting** packages (`Aspire.Hosting.*`, `CommunityToolkit.Aspire.Hosting.*`) are confined to **`AppHost/`**.

## Config order (later wins)

`appsettings.json` → `appsettings.Development.json` → Aspire env → user secrets

## Local workflow

1. Docker Desktop running
2. Run **AppHost** (full stack)
3. Aspire dashboard for logs/health; Seq for structured logs
4. Trust dev HTTPS cert on first setup

EF migrations apply on startup in Development.

## DON'TS

- ❌ No **Aspire client/component packages** in `Api`, `Application`, `Infrastructure`, or `ServiceDefaults` — AppHost provisions containers; services use native SDKs (`backend.md`)
- ❌ No documenting "run Api alone" as the primary dev path for full-stack work
- ❌ No secrets in AppHost source — user secrets / env
- ❌ No production CD assumptions — local template only
