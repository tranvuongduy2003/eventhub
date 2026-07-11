# Migration Workflow

Use this only after source scouting shows a schema change is required. `src/Infrastructure/AGENTS.md` owns migration rules.

## Before Generating

1. Read the owning domain/application behavior and EF configuration.
2. Confirm whether the change is additive, destructive, or data-shaping.
3. Check existing migrations and model snapshot naming style.
4. Plan rollback/data considerations for destructive or data-shaping changes.

## Generate And Review

Use the repository's existing EF tooling pattern discovered from project files and scripts. After generation, review the migration, designer, and snapshot for unintended table/column churn, nullable changes, delete behavior, indexes, defaults, and concurrency tokens.

If a REST shape changes with the schema, use `openapi-contract-sync` in the same change.

## Verification

```powershell
dotnet build EventHub.slnx -c Release
dotnet test EventHub.slnx -c Release
```
