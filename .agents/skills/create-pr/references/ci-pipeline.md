# Local CI Pipeline

Source: `.github/workflows/ci.yml`, job `build-and-test` on `ubuntu-latest`.

Run from repository root. Use `;` between commands in PowerShell.

**Critical:** Every step must exit 0. Do not skip required checks. Do not create a PR if any required step fails.

## Full Stack

```powershell
dotnet restore EventHub.slnx
dotnet format EventHub.slnx --verify-no-changes
dotnet build EventHub.slnx --no-restore -c Release
dotnet test EventHub.slnx --no-build -c Release --verbosity normal

yarn --cwd web install --frozen-lockfile
yarn --cwd web api:verify
yarn --cwd web lint
yarn --cwd web format:check
yarn --cwd web build
```

## Backend Only

When `web/` is untouched:

```powershell
dotnet restore EventHub.slnx
dotnet format EventHub.slnx --verify-no-changes
dotnet build EventHub.slnx --no-restore -c Release
dotnet test EventHub.slnx --no-build -c Release --verbosity normal

yarn --cwd web install --frozen-lockfile
yarn --cwd web api:verify
```

**Why `api:verify` even for backend-only?** Adding or changing API endpoints modifies the OpenAPI contract. `api:verify` catches this before remote checks do.

## Frontend Only

When no `src/` or `tests/` C# projects changed:

```powershell
yarn --cwd web install --frozen-lockfile
yarn --cwd web lint
yarn --cwd web format:check
yarn --cwd web build
```

## Common Fixes

| Failure | Action |
|---------|--------|
| `format:check` (web) | `yarn --cwd web format` then re-run check |
| `dotnet format` (Windows CRLF) | Remote checks use LF; document in PR if local-only |
| ESLint warnings on shadcn | Warnings often acceptable if remote checks pass |
| Vite Rolldown `INVALID_ANNOTATION` from SignalR | Non-fatal if build exits 0 |

## After Push

Remote checks run on the PR. Use GitHub MCP to inspect PR checks and summarize any failing job with the smallest relevant error lines.
