# Local CI pipeline (mirrors GitHub Actions)

Source: `.github/workflows/ci.yml` — job `build-and-test` on `ubuntu-latest`.

Run from **repository root**. Use `;` between commands in PowerShell.

## Full stack (default)

```powershell
dotnet restore EventHub.slnx
dotnet format EventHub.slnx --verify-no-changes
dotnet build EventHub.slnx --no-restore -c Release
dotnet test EventHub.slnx --no-build -c Release --verbosity normal

yarn --cwd web install --frozen-lockfile
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
```

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
