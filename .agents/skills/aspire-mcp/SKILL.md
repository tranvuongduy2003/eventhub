---
name: aspire-mcp
description: >
  Entry point for the aspire MCP server - query and interact with the running .NET
  Aspire AppHost (the local orchestration for the application starter stack) once it is started.
  Use to inspect the app model, list resources and their status, read service logs,
  discover endpoints/URLs, and run resource commands during local development.
  Triggers on: "aspire", "apphost", "orchestration", "resource status", "local dev
  stack", "service logs", "service endpoints", "is the backend running", "which URL
  is X on", "restart / inspect a resource".
---

# Aspire MCP

This skill is the entry point for the **`aspire`** MCP server (configured in `.codex/config.toml`,
launched as `aspire agent mcp`). Its tools are prefixed `mcp__aspire__*` and let an AI
agent query and interact with a running .NET Aspire AppHost - the local orchestrator for
this repo's services. The **exact tool list is discovered at runtime** from the MCP
server; do not assume tool names. The capabilities below mirror what the Aspire CLI
exposes (`aspire describe`, `aspire logs`, `aspire ps`, `aspire resource`, `aspire wait`),
which the `agent mcp` server surfaces to agents.

## What the AppHost orchestrates in this repo

The AppHost project is `src/AppHost/` (Aspire). It orchestrates the local dev stack:

- **`api`** - the .NET Minimal API backend.
- **`web`** - the React/Vite app on port 5000.
- **`postgres`** and **`app`** - PostgreSQL server and application database.
- **`cache`** - Redis cache.
- **`storage`** - MinIO object storage.
- **`seq`** - structured log viewer.
- **`seeder`** - data seeding project.

## When to use

Use this MCP once the AppHost is **already running** (started via `$aspire-mcp` or `powershell -NoProfile -ExecutionPolicy Bypass -File dotnet run --project src/AppHost/EventHub.AppHost.csproj`) to:

- **Check resource status** - see which resources are running, starting, failed, or stopped, and whether the stack came up healthy.
- **Read service logs** - pull logs for a specific resource (e.g. `api`, PostgreSQL) to debug a startup failure or runtime error.
- **Discover endpoints** - find the actual URLs/ports a resource is bound to (API, web, storage, database tools, dashboard) instead of guessing.
- **Inspect the app model** - enumerate resources and their relationships/configuration as the AppHost sees them.
- **Run resource commands** - trigger resource-level actions (e.g. restart) exposed by the running app.

Reach for it whenever a question is about the *running* orchestration ("is it up?", "why did X fail to start?", "what URL is the backend on?", "show me the fake API's logs").

## How it complements `$aspire-mcp`

- Start the AppHost with `dotnet run --project src/AppHost/EventHub.AppHost.csproj` before using this skill; the MCP tools need a running AppHost to talk to.
- **This MCP** is for **querying and interacting** with that already-running AppHost: status, logs, endpoints, resource commands.

If the MCP tools report no running AppHost or cannot connect, the stack is not up. Start AppHost before retrying. Do not use this skill to start the stack.

## Workflow

1. Ensure the AppHost is running (`$aspire-mcp` if not).
2. List/inspect resources to get their names and status.
3. For a problem resource, pull its logs and read the failing endpoints.
4. Use endpoint discovery to get exact URLs (e.g. before driving the web with the `chrome-devtools` MCP).
5. Use resource commands only when an explicit action (like a restart) is needed.

## Notes

- Match the resource names above when referring to resources.
- Logs and endpoints may surface real request/response data; never copy sensitive user data into committed files, comments, or reports.
- For building/configuring the AppHost or backend code, follow `docs/technical.md and src/AGENTS.md`; this skill is only about interacting with the running instance.







