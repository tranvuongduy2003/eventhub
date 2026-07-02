# EventHub

Local-first event management and ticketing platform. .NET backend (Clean Architecture + CQRS + DDD) and React frontend. Orchestrated by [.NET Aspire](https://aspire.dev).

## About

**EventHub** connects organizers and attendees for small events — transparent pricing, valid tickets, check-in, and basic results. Built as a pet project with Codex agent configuration.

### Codex agent setup (`.codex/`)

| Piece | Purpose |
|-------|---------|
| **Project config** (`.codex/`) | Hooks, permissions, MCP servers, custom agents |
| **Skills** (`.agents/skills/`) | OpenAPI sync, MCP (Postgres, Neo4j GraphRAG), env setup, git/PR, UI |
| **Custom agents** (`.codex/agents/`) | Read-only subagents and workflow helpers |

Open the repo in Codex; agents read `AGENTS.md` and **`docs/CONSTITUTION.md`** plus companion docs before changing code.

**Agent workflow:** `/spec` (spec in `docs/_memory/specs/` + one GitHub issue) → `/plan` (agent skills manage implementation) → `/cook` (implement, then delete plan if one was created).

### Stack highlights

- **Modular monolith** — bounded contexts in [`docs/_memory/source/domain-model-specification.md`](docs/_memory/source/domain-model-specification.md)
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

Shared MCP server config lives in [`.codex/config.toml`](.codex/config.toml). Do not use `.mcp.json` as a repository standard; it is local-only and may contain machine-specific secrets.

| Server | Purpose |
|--------|---------|
| `aspire` | Aspire dashboard resources, logs |
| `postgres` | Read-only SQL against local `app` database |
| `neo4j-graphrag` | Cypher, vector/fulltext search, GraphRAG |
| `playwright` | Browser automation for e2e diagnostics |
| `github` | GitHub MCP over HTTP |
| `shadcn` | shadcn component registry MCP |

See `.agents/skills/postgres-mcp/SKILL.md` and `.agents/skills/neo4j-graphrag/SKILL.md`.

## Docs

| Document | Role |
|----------|------|
| [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) | Immutable principles |
| [`docs/_memory/source/product-requirements.md`](docs/_memory/source/product-requirements.md) | Product intent and decisions |
| [`docs/_memory/source/feature-specification.md`](docs/_memory/source/feature-specification.md) | Epics, features, acceptance criteria |
| [`docs/_memory/source/domain-model-specification.md`](docs/_memory/source/domain-model-specification.md) | Domain model |
| [`docs/_memory/source/technical-design.md`](docs/_memory/source/technical-design.md) | Architecture and infrastructure |
| [`docs/_memory/specs/`](docs/_memory/specs/) | Product specs (committed) |

Ephemeral plans live in `.codex/plans/` (gitignored; deleted after `/cook`).

## API contract

```bash
yarn --cwd web api:export
yarn --cwd web api:codegen
```

See [`contracts/openapi/README.md`](contracts/openapi/README.md).
