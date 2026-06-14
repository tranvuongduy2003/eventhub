# Architecture Decisions

Accepted decisions for this boilerplate. Add a row when making significant architecture choices.

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-06-07 | Convert repo to CA + CQRS + DDD boilerplate | Remove product domain; keep patterns and Cursor workflows |
| 2026-05-22 | Cookie session auth with Redis cache + PostgreSQL authority | Simple local MVP; cache rebuildable from DB |
| 2026-05-22 | MediatR pipeline with UoW + domain event dispatch after commit | Keeps handlers thin; consistent transaction boundaries |
| 2026-05-22 | `AssemblyReference.cs` per project for discovery | Avoid `GetEntryAssembly()` fragility in tests and hosts |
