---
description: "DataSeeder console app — structure, conventions, and how to extend seed data. Use when working with src/DataSeeder/, scripts/Seed.ps1, or Data/*.json files."
paths:
  - "src/DataSeeder/**"
  - "scripts/Seed.ps1"
---

# DATA SEEDER

Console app that seeds the PostgreSQL database. Independent of Api — runs directly against the DB via EF Core.

## Run

```powershell
.\scripts\Seed.ps1
```

Or directly:

```bash
dotnet run --project src/DataSeeder/EventHub.DataSeeder.csproj
```

Idempotent — each seeder checks `AnyAsync()` before inserting. Safe to re-run.

## Structure

```
src/DataSeeder/
├── Program.cs                  ← thin orchestrator: migrations → seeders
├── Helpers/
│   ├── ConnectionStringResolver.cs  ← env var → appsettings.json fallback
│   └── JsonLoader.cs               ← generic Load<T>(directory, file, options)
├── Seeders/
│   ├── UserSeeder.cs               ← Users.json → UserRecord (password hash)
│   ├── PermissionSeeder.cs         ← Permissions.json → console log (reference data)
│   ├── RoleSeeder.cs               ← Roles.json → console log (reference data)
│   └── EventUserRoleSeeder.cs      ← EventUserRoles.json → EventUserRoleRecord
├── Models/                         ← deserialization records (one per JSON file)
└── Data/                           ← JSON seed files (CopyToOutputDirectory)
```

## Conventions

- Each seeder is a `static class` with `SeedAsync` method.
- Guard: `if (await dbContext.X.AnyAsync()) return;` — skip if already seeded.
- JSON seed files in `Data/` — models in `Models/`, one-to-one mapping.
- `Helpers/` for cross-cutting utilities only (JSON loading, connection string).
- **No domain logic** — seeder maps JSON → EF records directly.
- No comments in code.

## How to extend

1. Add JSON file to `Data/` (already copied to output via `.csproj` glob).
2. Add model to `Models/`.
3. Add seeder to `Seeders/` with `AnyAsync()` guard.
4. Call from `Program.cs`.

## Seed data (current)

| File | Entries | Target |
|------|---------|--------|
| `Users.json` | 100 (30 Organizer, 70 Attendee) | `UserRecord` |
| `Permissions.json` | 5 | Console log (reference) |
| `Roles.json` | 2 (Owner, Staff) | Console log (reference) |
| `EventUserRoles.json` | 48 (10 Owner, 38 Staff) | `EventUserRoleRecord` |

Authorization model: Owner → all 5 permissions; Staff → CheckIn + Reporting only. See `EventRolePermissions` in Domain.
