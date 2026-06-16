---
name: postgres-mcp
description: Inspects and queries the local PostgreSQL database via MCP (read-only). Use when verifying data, debugging persistence, exploring schema, writing SELECT diagnostics, or when the user mentions Postgres MCP, database rows, or SQL inspection.
---

# Postgres MCP (EventHub)

Read-only PostgreSQL access through the **`postgres`** MCP server in [`.mcp.json`](../../../.mcp.json).

## Connection string (source of truth: AppHost)

Credentials and ports are defined by the Aspire AppHost, not hardcoded ad hoc values.

| Setting | AppHost source | Local dev value |
|---|---|---|
| Postgres resource | `AddPostgres("postgres", …)` in [`src/AppHost/AppHost.cs`](../../../src/AppHost/AppHost.cs) | `postgres` |
| Database | `AddDatabase("app")` | `app` → services use `ConnectionStrings__app` |
| Username | Aspire Postgres container default | `postgres` |
| Password | `Parameters:postgres-password` in [`src/AppHost/appsettings.Development.json`](../../../src/AppHost/appsettings.Development.json) | `postgres` |
| Host port | `.WithEndpoint(port: 5432, targetPort: 5432)` | `5432` |

**MCP URI** (must stay in sync with AppHost):

```
postgresql://postgres:postgres@localhost:5432/app?sslmode=disable
```

Also documented in [`.env.example`](../../../.env.example) as `POSTGRES_URI`. If you change `Parameters:postgres-password` or the database name in AppHost, update `.mcp.json`, `.env.example`, and `.env` together.

When the stack is running, confirm the live string in the **Aspire dashboard** (Postgres → `app` connection string) or via **aspire** MCP `list_resources`.

## Configuration

```json
"postgres": {
  "command": "npx",
  "args": [
    "-y",
    "@modelcontextprotocol/server-postgres",
    "postgresql://postgres:postgres@localhost:5432/app?sslmode=disable"
  ]
}
```

| Item | Value |
|---|---|
| Server id | `postgres` |
| Package | `@modelcontextprotocol/server-postgres` |
| Mode | **Read-only** (all SQL runs in a read-only transaction) |
| Prerequisites | AppHost running with Docker; Postgres reachable at the URI above |

If MCP connection fails, compare the URL to the Aspire dashboard connection string for database **`app`**.

## When to use MCP vs other skills

| Task | Use |
|---|---|
| Inspect live rows, counts, joins, explain plans | **This skill** → MCP `query` |
| Table/column meaning, invariants, indexes | [`docs/technical.md`](../../../docs/technical.md) §6 and [`docs/ddd.md`](../../../docs/ddd.md) first, then MCP to confirm |
| Add/change schema, EF migrations | Constitution III · Tech §6 · `migration.md` — **not** MCP (no writes) |
| AppHost / container not up | `aspire.md`, `env-doctor` |
| Integration tests | Constitution VII · Tech §10 · `backend-testing.md` |

## MCP capabilities

### Tool: `query`

- **Input:** `sql` — a **SELECT** (or read-only diagnostic such as `EXPLAIN` if the server allows it)
- **No** `INSERT`, `UPDATE`, `DELETE`, `DDL`, or migration scripts via MCP

### Resources (schema)

The server exposes per-table schema resources (URI pattern like `postgres://…/schema`). Prefer reading schema resources or `information_schema` before guessing column names.

## Project conventions (always apply in SQL)

From [`docs/technical.md`](../../../docs/technical.md) §6 and [`docs/ddd.md`](../../../docs/ddd.md):

- Application schema: **`app`** (not `public` for app tables)
- Qualify tables: `app.users`, `app.user_sessions`, …
- Indexes: `ux_users_username`, `ux_users_email`

Core tables: `users`, `user_sessions`.

## Workflow

1. Confirm Postgres is up (Aspire dashboard or `env-doctor`).
2. Read [`docs/technical.md`](../../../docs/technical.md) §6 and [`docs/ddd.md`](../../../docs/ddd.md) for the question (FKs, indexes, aggregates).
3. Call MCP **`query`** with qualified `app.*` SQL.
4. Interpret results against Constitution I–III and Tech §5–6 — MCP shows storage, not business validation.

## Safety

- Local dev only; connection string contains credentials — never commit secrets beyond the existing dev URL pattern.
- Do not paste large result sets into chat; summarize counts and sample rows.
- Redis is **not** in this MCP server — use docs / app logs for session cache projections.

## Examples

See [reference.md](reference.md) for starter queries.
