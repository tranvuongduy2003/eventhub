# EventHub

EventHub is a small event management and ticketing platform. It uses a .NET Clean Architecture + CQRS + DDD backend, a React + TypeScript + Vite frontend, and .NET Aspire for the local topology.

## About

EventHub connects organizers and attendees for small events: transparent pricing, valid tickets, check-in, and basic results. It is a pet project focused on a complete, maintainable end-to-end product rather than large-scale ticketing.

## Codex Agent Setup

| Piece | Purpose |
| --- | --- |
| `AGENTS.md` and nested `AGENTS.md` files | Repository and path-scoped working rules |
| `.agents/skills/` | Reusable workflows for backend, frontend, E2E, OpenAPI, MCP, and harness work |
| `.codex/agents/` | Focused subagent prompts for review, security, acceptance, implementation, and harness diagnosis |
| `.codex/hooks.json` and `.codex/hooks/` | Guard, formatting, telemetry, and stop-verification hooks |
| `scripts/agent/` | Current validation sensors: changed-code, docs, and harness policy |

Agents should read the smallest relevant set of `docs/product.md`, `docs/features.md`, and `docs/technical.md`, then the applicable `AGENTS.md` files and skill instructions before changing code.

## Stack Highlights

- Modular monolith with bounded contexts and invariants in `docs/technical.md`.
- PostgreSQL is authoritative; Redis is rebuildable cache; MinIO stores binary assets.
- RabbitMQ is the integration-event broker for asynchronous bounded-context work.
- React 19 + Vite frontend consumes generated OpenAPI types.
- .NET Aspire AppHost is the local topology source of truth. Do not add hand-authored Docker Compose.

## Prerequisites

- .NET 10 SDK
- Aspire CLI 13.3+
- Node.js 22 LTS and Yarn
- Docker Desktop or a compatible container runtime

## First-Time Setup

From the repository root:

```powershell
.\scripts\Setup-Environments.ps1
```

Or manually:

```powershell
dotnet restore EventHub.slnx
yarn --cwd web install
dotnet dev-certs https
dotnet dev-certs https --trust
```

Create local `.env` files from the examples when needed. Do not commit secrets or machine-specific environment files.

## Run Locally

```powershell
dotnet run --project src/AppHost/EventHub.AppHost.csproj
```

Aspire exposes the API, web app, PostgreSQL, Redis, MinIO, RabbitMQ, Seq, and seeder resources through the dashboard.

## MCP Servers

Shared MCP server config lives in `.codex/config.toml`.

| Server | Purpose |
| --- | --- |
| `aspire` | Inspect running Aspire resources, logs, and endpoints |
| `postgres` | Read-only SQL diagnostics against local `app` database |
| `playwright` | Browser automation for E2E diagnostics |
| `github` | GitHub repository, PR, and issue automation |
| `shadcn` | shadcn component registry MCP |

## Docs

| Document | Role |
| --- | --- |
| `docs/product.md` | Product intent, scope, decisions, guardrails |
| `docs/features.md` | Epics, features, acceptance criteria, delivery status |
| `docs/technical.md` | Architecture, domain model, invariants, runtime, verification |
| `docs/specs/` | Committed implementation specs |
| `docs/harness/` | Codex harness operating notes |

Ephemeral plans, state, and telemetry live under ignored `.codex/` scratch paths.

## Common Verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
dotnet build EventHub.slnx -c Release
dotnet test EventHub.slnx -c Release
yarn --cwd web api:verify
yarn --cwd web build
yarn --cwd e2e test
```

## API Contract

```powershell
yarn --cwd web api:export
yarn --cwd web api:codegen
```

See `contracts/openapi/README.md`.
