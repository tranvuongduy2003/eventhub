# EventHub — Agent instructions

**EventHub** is a local-first event management and ticketing platform (.NET Clean Architecture + CQRS + DDD, React 19 + Vite, .NET Aspire).

## Start here

1. Read [`docs/constitution.md`](docs/constitution.md) — non-negotiable invariants.
2. Read [`.opencode/rules/core.mdc`](.opencode/rules/core.mdc) — doc routing, repo layout, slash commands, subagents.
3. Read [`.opencode/notes/progress.md`](.opencode/notes/progress.md) if present — current goal, decisions, next steps.

## Workflow

| Step | Command | Output |
|------|---------|--------|
| Spec | `/spec` | `docs/specs/<timestamp>-<name>.md` |
| Plan | `/plan` | `.opencode/plans/<same-filename>.md` (gitignored, ephemeral) |
| Build | `/build` | Production code + green checks; delete plan when done |

Command definitions: [`.opencode/commands/`](.opencode/commands/).

## Harness

Deterministic guards and verification run via [`.opencode/plugins/harness.ts`](.opencode/plugins/harness.ts) → PowerShell scripts in [`.opencode/hooks/`](.opencode/hooks/). See [`.opencode/rules/harness.mdc`](.opencode/rules/harness.mdc).

## Subagents

OpenCode **primary** agents: `build` (default), `plan` (Tab to switch). **Subagents**: built-in `@explore`, `@general`, `@scout`, plus project agents in [`.opencode/agents/`](.opencode/agents/).

Invoke project agents with **`@<name>`** or the **`task`** tool from **build**. Parallel readonly scouts OK; **one writer** (build) for production code.

## Skills

On-demand procedures in [`.opencode/skills/*/SKILL.md`](.opencode/skills/). Load via the `skill` tool when the task matches.

## MCP

Project MCP servers are declared in [`opencode.json`](opencode.json) using `{env:VAR}` substitution — set `POSTGRES_URI`, `NEO4J_*`, and provider keys in [`.env`](.env.example) (copy from `.env.example`). Copy [`.mcp.json.example`](.mcp.json.example) to `.mcp.json` only if another tool still needs the legacy format.
