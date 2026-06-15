# EventHub

Local-first event management and ticketing platform. .NET backend (Clean Architecture + CQRS + DDD) and React frontend. Orchestrated by [.NET Aspire](https://aspire.dev).

## About

**EventHub** connects organizers and attendees for small events — transparent pricing, valid tickets, check-in, and basic results. Built as a pet project with Cursor-native agent configuration.

### Cursor agent setup (`.cursor/`)

| Piece | Purpose |
|-------|---------|
| **Rules** (`rules/`) | Layer boundaries, CQRS, Aspire, API contracts, testing, frontend |
| **Skills** (`skills/`) | OpenAPI sync, MCP (Postgres, Neo4j GraphRAG), env setup, git/PR, UI |
| **Commands** (`commands/`) | `/spec` → `/plan` → `/build` |

Open the repo in [Cursor](https://cursor.com); agents read `core.mdc` and **`docs/constitution.md`** plus companion docs before changing code.

**Agent workflow:** `/spec` (spec in `docs/specs/` + one GitHub issue) → `/plan` (ephemeral plan in `.cursor/plans/`, not committed) → `/build` (implement, then delete plan).

### Stack highlights

- **Modular monolith** — bounded contexts in [`docs/ddd.md`](docs/ddd.md)
- **PostgreSQL** (authoritative) + **Redis** (cache) + **MinIO** (images) + **RabbitMQ** (integration events)
- **React 19 + Vite** frontend with OpenAPI → TypeScript codegen
- **.NET Aspire** AppHost — no hand-authored `docker-compose`

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://aspire.dev) 13.3+
- [Node.js 22 LTS](https://nodejs.org/) and [Yarn](https://yarnpkg.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [uv](https://docs.astral.sh/uv/) (optional — for Neo4j GraphRAG MCP via `uvx`)

## First-time setup

From the repository root (Windows PowerShell):

```powershell
.\scripts\Setup-Environments.ps1
```

Or manually:

```bash
dotnet restore EventHub.slnx
yarn --cwd web install

cp .env.example .env
cp web/.env.example web/.env
cp .mcp.json.example .mcp.json

dotnet dev-certs https
dotnet dev-certs https --trust
```

## Run locally

```bash
dotnet run --project src/AppHost/EventHub.AppHost.csproj
```

Or:

```bash
aspire run --project src/AppHost/EventHub.AppHost.csproj
```

| Service | URL / port |
|---------|------------|
| Web (Vite) | `https://localhost:5000` |
| API (HTTPS) | `https://localhost:8000` |
| PostgreSQL | `localhost:5432` |
| Redis | `localhost:6379` |
| MinIO | Aspire dashboard (resource `storage`) |
| RabbitMQ | Aspire dashboard (resource `messaging`) |
| Seq | Aspire dashboard (resource `seq`) |

## MCP servers

Copy [`.mcp.json.example`](.mcp.json.example) to `.mcp.json` and set credentials in `.env`:

| Server | Purpose |
|--------|---------|
| `aspire` | Aspire dashboard resources, logs |
| `postgres` | Read-only SQL against local `app` database |
| `neo4j-graphrag` | Cypher, vector/fulltext search, GraphRAG |

See `.cursor/skills/postgres-mcp/SKILL.md` and `.cursor/skills/neo4j-graphrag/SKILL.md`.

## Docs

| Document | Role |
|----------|------|
| [`docs/constitution.md`](docs/constitution.md) | Immutable principles |
| [`docs/prd.md`](docs/prd.md) | Product intent and decisions |
| [`docs/features.md`](docs/features.md) | Epics, features, acceptance criteria |
| [`docs/ddd.md`](docs/ddd.md) | Domain model |
| [`docs/technical.md`](docs/technical.md) | Architecture and infrastructure |
| [`docs/specs/`](docs/specs/) | Product specs (committed) |

Ephemeral plans live in `.cursor/plans/` (gitignored; deleted after `/build`).

## API contract

```bash
yarn --cwd web api:export
yarn --cwd web api:codegen
```

See [`contracts/openapi/README.md`](contracts/openapi/README.md).
