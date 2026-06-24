# Local CI pipeline (mirrors GitHub Actions)

Source: `.github/workflows/ci.yml` — job `build-and-test` on `ubuntu-latest`.

Run from **repository root**. Use `;` between commands in PowerShell.

**CRITICAL: Every step must exit 0. Do not skip any step. Do not create a PR if any step fails.**

## Full stack (default)

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

## Backend only

When `web/` is untouched:

```powershell
dotnet restore EventHub.slnx
dotnet format EventHub.slnx --verify-no-changes
dotnet build EventHub.slnx --no-restore -c Release
dotnet test EventHub.slnx --no-build -c Release --verbosity normal

yarn --cwd web install --frozen-lockfile
yarn --cwd web api:verify
```

**Why `api:verify` even for backend-only?** Adding/changing API endpoints modifies the OpenAPI contract. `api:verify` catches this before CI does.

## Frontend only

When no `src/` or `tests/` C# projects changed:

```powershell
yarn --cwd web install --frozen-lockfile
yarn --cwd web lint
yarn --cwd web format:check
yarn --cwd web build
```

## Common fixes

| Failure | Action |
|---------|--------|
| `format:check` (web) | `yarn --cwd web format` then re-run check |
| `dotnet format` (Windows CRLF) | CI uses LF; document in PR if local-only |
| ESLint warnings on shadcn | Warnings often acceptable if CI passes |
| Vite Rolldown `INVALID_ANNOTATION` from SignalR | Non-fatal if build exits 0 |

## After push

GitHub re-runs the same workflow on the PR. Use `gh pr checks <number>` or the PR Checks tab to confirm.
