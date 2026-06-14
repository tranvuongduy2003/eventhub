# Clean Architecture + CQRS + DDD Boilerplate

Local-first template with .NET backend and React frontend. Orchestrated by [.NET Aspire](https://aspire.dev).

## About

A **Cursor-native boilerplate** for building .NET backends with AI-assisted development. You get a working Clean Architecture codebase *and* the agent configuration to extend it correctly—rules, skills, commands, and memory artifacts wired to this repo's layout and conventions.

### Cursor agent setup (`.cursor/`)

| Piece | Purpose |
|-------|---------|
| **Rules** (`rules/`) | Always-on and scoped guidance—layer boundaries, CQRS, Aspire, API contracts, testing, frontend |
| **Skills** (`skills/`) | Procedural playbooks only (OpenAPI sync, MCP, env setup, git/PR, frontend UI) — architecture lives in `docs/` |
| **Commands** (`commands/`) | Slash workflows: `/spec`, `/plan`, `/build`, `/epic-review` |
| **Memory** (`docs/memory/`) | Session status, architecture decisions, known issues for long-running agent work |

Open the repo in [Cursor](https://cursor.com); agents read `core.mdc` and **`docs/CONSTITUTION.md` + `docs/TECHNICAL.md`** before changing code. Skills cover workflows docs do not spell out.

### Application template

The sample stack demonstrates **Clean Architecture**, **CQRS** (MediatR), and **DDD** (aggregates, value objects, domain events):

- **`User` bounded context** — registration, login, cookie session auth
- **PostgreSQL** (authoritative) + **Redis** (rebuildable session cache)
- **React 19 + Vite** frontend with OpenAPI → TypeScript codegen
- **.NET Aspire** AppHost — PostgreSQL, Redis, API, and web; no hand-authored `docker-compose`

**Included:** layered solution, integration tests, Aspire orchestration, contract-driven API types.

**Out of scope:** production deployment, message brokers, transactional outbox, horizontal scaling.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://aspire.dev) 13.3+
- [Node.js 22 LTS](https://nodejs.org/) and [Yarn](https://yarnpkg.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (PostgreSQL, Redis containers)

## First-time setup

From the repository root (Windows PowerShell):

```powershell
.\scripts\Setup-Environments.ps1
```

Or manually:

```bash
dotnet restore Solution.slnx
yarn --cwd web install

cp .env.example .env
cp web/.env.example web/.env

dotnet dev-certs https
dotnet dev-certs https --trust
```

## Run locally

### Full stack (Aspire)

Start PostgreSQL, Redis, API, and Vite:

```bash
dotnet run --project src/AppHost/Solution.AppHost.csproj
```

Or with the Aspire CLI:

```bash
aspire run --project src/AppHost/Solution.AppHost.csproj
```

| Service | URL / port |
|---------|------------|
| Web (Vite) | `https://localhost:5000` |
| API (HTTPS) | `https://localhost:8000` |
| API (HTTP) | `http://localhost:8001` |
| PostgreSQL | `localhost:5432` |
| Redis | `localhost:6379` |

Aspire injects `ConnectionStrings__app` and `ConnectionStrings__cache` into the Api, and sets `VITE_API_URL` for the `web` resource.

### Frontend only (`yarn`)

Use this instead of the AppHost `web` resource (do not run both on port 5000):

```bash
cp web/.env.example web/.env   # once
yarn --cwd web dev
```

## Solution layout

```
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts
tests/     Unit and integration test projects
web/       React 19 + Vite frontend (outside .slnx)
```

## API contract (OpenAPI → TypeScript)

REST shapes are in [`contracts/openapi/api.v1.yaml`](contracts/openapi/api.v1.yaml). The web app generates types into `web/src/generated/` (gitignored).

After changing API endpoints:

```bash
yarn --cwd web api:export
yarn --cwd web api:codegen
```

CI runs `yarn --cwd web api:verify`. See [`contracts/openapi/README.md`](contracts/openapi/README.md).

## Docs

- [`docs/PRD.md`](docs/PRD.md) — boilerplate scope
- [`docs/TECHNICAL.md`](docs/TECHNICAL.md) — architecture and persistence
- [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) — immutable principles
