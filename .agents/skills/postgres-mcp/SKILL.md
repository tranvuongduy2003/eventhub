---
name: postgres-mcp
description: Inspects and queries the local PostgreSQL database via the Codex MCP server (read-only). Use when verifying data, debugging persistence, exploring schema, writing SELECT diagnostics, or when the user mentions Postgres MCP, database rows, or SQL inspection.
---

# Postgres MCP (EventHub)

Read-only PostgreSQL access through the **`postgres`** MCP server in [`.codex/config.toml`](../../../.codex/config.toml).

## Connection string (source of truth: AppHost)

Credentials and ports are defined by the Aspire AppHost, not hardcoded ad hoc values.

| Setting | AppHost source | Local dev value |
|---|---|---|
| Postgres resource | `AddPostgres("postgres", â€¦)` in [`src/AppHost/AppHost.cs`](../../../src/AppHost/AppHost.cs) | `postgres` |
| Database | `AddDatabase("app")` | `app` â†’ services use `ConnectionStrings__app` |
| Username | Aspire Postgres container default | `postgres` |
| Password | `Parameters:postgres-password` in [`src/AppHost/appsettings.Development.json`](../../../src/AppHost/appsettings.Development.json) | `postgres` |
| Host port | `.WithEndpoint(port: 5432, targetPort: 5432)` | `5432` |

**MCP URI** (must stay in sync with AppHost):

```
postgresql://postgres:postgres@localhost:5432/app?sslmode=disable
```

Also documented in [`.env.example`](../../../.env.example) as `POSTGRES_URI`. If you change `Parameters:postgres-password` or the database name in AppHost, update `.codex/config.toml`, `.env.example`, and `.env` together.

When the stack is running, confirm the live string in the **Aspire dashboard** (Postgres â†’ `app` connection string) or via **aspire** MCP `list_resources`.

## Configuration

```toml
[mcp_servers.postgres]
command = "npx"
args = ["-y", "@modelcontextprotocol/server-postgres", "postgresql://postgres:postgres@localhost:5432/app?sslmode=disable"]
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
| Inspect live rows, counts, joins, explain plans | **This skill** â†’ MCP `query` |
| Table/column meaning, invariants, indexes | [`docs/_memory/source/technical-design.md`](../../../docs/_memory/source/technical-design.md) Â§6 and [`docs/_memory/source/domain-model-specification.md`](../../../docs/_memory/source/domain-model-specification.md) first, then MCP to confirm |
| Add/change schema, EF migrations | Constitution III · Tech §6 — **not** MCP (no writes) |
| AppHost / container not up | `aspire.md`, `env-doctor` |
| Integration tests | Constitution VII · Tech §11 |

## MCP capabilities

### Tool: `query`

- **Input:** `sql` â€” a **SELECT** (or read-only diagnostic such as `EXPLAIN` if the server allows it)
- **No** `INSERT`, `UPDATE`, `DELETE`, `DDL`, or migration scripts via MCP

### Resources (schema)

The server exposes per-table schema resources (URI pattern like `postgres://â€¦/schema`). Prefer reading schema resources or `information_schema` before guessing column names.

## Project conventions (always apply in SQL)

From [`docs/_memory/source/technical-design.md`](../../../docs/_memory/source/technical-design.md) Â§6 and [`docs/_memory/source/domain-model-specification.md`](../../../docs/_memory/source/domain-model-specification.md):

- Application schema: **`app`** (not `public` for app tables)
- Qualify tables: `app.users`, `app.user_sessions`, â€¦
- Indexes: `ux_users_username`, `ux_users_email`

Core tables: `users`, `user_sessions`.

## Workflow

1. Confirm Postgres is up (Aspire dashboard or `env-doctor`).
2. Read [`docs/_memory/source/technical-design.md`](../../../docs/_memory/source/technical-design.md) Â§6 and [`docs/_memory/source/domain-model-specification.md`](../../../docs/_memory/source/domain-model-specification.md) for the question (FKs, indexes, aggregates).
3. Call MCP **`query`** with qualified `app.*` SQL.
4. Interpret results against Constitution Iâ€“III and Tech Â§5â€“6 â€” MCP shows storage, not business validation.

## Safety

- Local dev only; connection string contains credentials â€” never commit secrets beyond the existing dev URL pattern.
- Do not paste large result sets into chat; summarize counts and sample rows.
- Redis is **not** in this MCP server â€” use docs / app logs for session cache projections.

## Examples

See [reference.md](reference.md) for starter queries.
