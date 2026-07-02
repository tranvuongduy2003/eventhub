---
name: env-doctor
description: Diagnoses and fixes local development environment issues for EventHub (.NET Aspire, Docker, PostgreSQL, Redis, MinIO, RabbitMQ, Node/Vite). Use when setup fails, prerequisites are missing, Docker or Aspire won't start, connection errors appear, or the user mentions env, environment, prerequisites, or "can't run locally".
---

# Env Doctor

Systematically verify and repair the local dev environment for this repository. Run checks yourself with shell commands; do not ask the user to run diagnostics you can run.

## When to use

- First-time setup or onboarding
- `dotnet run`, AppHost, or Aspire dashboard failures
- Docker, PostgreSQL, or Redis connection errors
- Frontend dev server or Vite env issues
- "Works on my machine" / version mismatch suspicions

## When not to use

- Application logic bugs with a healthy environment (use debugging skills instead)
- Production or cloud deployment (MVP is local-only per `docs/_memory/source/technical-design.md`)

## Workflow

Copy this checklist and update as you go:

```
Env Doctor Progress:
- [ ] 1. Capture symptom and context
- [ ] 2. Run prerequisite checks
- [ ] 3. Run project health checks
- [ ] 4. Apply fixes (smallest change first)
- [ ] 5. Verify end-to-end
- [ ] 6. Report findings
```

### 1. Capture symptom and context

Record:

- Exact error message or failing command
- OS and shell (Windows/macOS/Linux)
- Whether Docker Desktop was running before the failure
- What changed recently (SDK upgrade, branch switch, clean clone)

### 2. Prerequisite checks

Run version and availability commands. Expected baseline from `docs/_memory/source/technical-design.md` Â§10 and README prerequisites:

| Tool | Expected |
|------|----------|
| .NET SDK | .NET 10 |
| Node.js | LTS |
| Package manager | Yarn (`web/yarn.lock`) |
| Docker | Engine reachable (Docker Desktop on Windows/macOS) |

Suggested commands (adapt if a tool is missing):

```bash
dotnet --version
dotnet --list-sdks
node --version
yarn --version
docker version
docker info
```

If Aspire workload is required and `dotnet workload list` shows gaps, note them; install only with user consent.

### 3. Project health checks

From repo root, verify structure and dependencies exist:

```bash
# .NET restore (solution may be .slnx once code lands)
dotnet restore

# Frontend (when web/ exists)
yarn --cwd web install --frozen-lockfile
```

Check for:

- `src/AppHost/` present and buildable
- `web/package.json` and lockfile consistency
- No obvious port conflicts for common Aspire ports (dashboard, Postgres, Redis, API, Vite)
- Dev HTTPS cert: `dotnet dev-certs https --check` (trust with `--trust` if check fails and user agrees)

Do **not** commit `.env` files or paste secrets into chat. Aspire injects connection strings at runtime; missing manual `.env` is normal for backend services.

### 4. Apply fixes (smallest first)

Prefer reversible, documented fixes:

| Symptom | Likely fix |
|---------|------------|
| Docker not running | Start Docker Desktop; wait until `docker info` succeeds |
| SDK version mismatch | Install .NET 10 SDK; pin via `global.json` if repo adds one |
| Restore failures | Clear NuGet cache, re-run `dotnet restore` |
| Frontend install errors | Delete `web/node_modules`, reinstall with the lockfile's package manager |
| Cert / HTTPS errors | `dotnet dev-certs https --trust` (Windows may need elevated shell) |
| Stale containers | `docker ps -a`; remove orphaned Aspire containers only when safe |
| Aspire resource unhealthy | Open Aspire dashboard logs; restart AppHost |

Escalate before destructive actions: wiping Docker volumes, `--force` git operations, or global machine config changes.

### 5. Verify end-to-end

Success criteria for this project:

1. `dotnet build` succeeds for the solution
2. AppHost starts without immediate crash
3. Aspire dashboard shows PostgreSQL, Redis, MinIO, RabbitMQ, Seq, API, and web resources healthy
4. Frontend loads and can reach the API URL Aspire injects into Vite

If full run is too heavy, confirm at minimum: prerequisites pass + `dotnet build` + Docker healthy.

### 6. Report findings

Use this template:

```markdown
## Env Doctor Report

### Symptom
[What the user saw]

### Root cause
[One sentence]

### Checks run
- [Tool versions / commands and outcomes]

### Fixes applied
- [What changed, or "none â€” environment already healthy"]

### Verification
- [What was re-run and result]

### Follow-up (if any)
- [Manual steps only you could not run]
```

## Project-specific notes

- **Orchestration**: .NET Aspire AppHost is the single local entry point; Docker Compose is not used.
- **Configuration**: Layered `appsettings` + Aspire-injected env vars + user secrets; see `docs/_memory/source/technical-design.md` Â§8.
- **Database**: EF migrations apply on dev startup; migration CLI issues are separate from env prerequisites.
- **CI vs local**: GitHub Actions may use different agents; compare versions if "CI passes, local fails."

## Additional resources

- Detailed per-OS and tooling checks: [resources/checklist.md](resources/checklist.md)
- Architecture and prerequisites: `docs/_memory/source/technical-design.md` Â§10, README

