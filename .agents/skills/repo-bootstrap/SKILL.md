---
name: repo-bootstrap
description: Check the EventHub repo environment before substantial work. Use when opening a fresh workspace, after dependency/tooling changes, or when local commands fail in a way that may be environmental.
---

# Repo Bootstrap

Establish whether the workspace can support normal EventHub development without changing source code.

## Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Repo-Bootstrap.ps1
```

Use `-Json` when another script or agent needs structured output.

## What It Checks

- Required tools: `dotnet`, `node`, `yarn`, `git`, `aspire`
- Required repo contracts: `AGENTS.md`, `EventHub.slnx`, Aspire projects, web package, affected-test map, harness manifest, harness policy
- Next commands to use for verification and local topology

## Rules

- Read-only by default; do not install dependencies from this skill.
- If a tool is missing, report the missing tool and stop before deep debugging.
- Local topology is Aspire AppHost; do not introduce `docker-compose.yml`.
- Secrets remain local. Do not edit `.env`, `.env.*`, or `.mcp.json`.

## Output Contract

Report:

- bootstrap status
- missing tools or missing repo files
- next verification command

