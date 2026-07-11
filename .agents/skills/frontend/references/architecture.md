# Frontend Architecture Workflow

Use this reference when changing routes, module boundaries, shared app wiring, or file placement. Rules live in `web/AGENTS.md`; this file is a source-reading workflow.

## Scout

Read the closest existing pattern first:

- Route composition: `web/src/app/router.tsx`.
- Route path constants: `web/src/app/paths.ts`.
- Guard composition: `web/src/app/routes/protected-route.tsx` and `web/src/app/routes/public-route.tsx`.
- Shared shells: `web/src/layouts`.
- Feature-owned workflows: `web/src/features/<feature>`.
- Shared API plumbing: `web/src/lib/api`.

Useful commands:

```powershell
rg "createBrowserRouter|paths\.|ProtectedRoute|PublicRoute" web/src/app web/src/features
rg --files web/src/features/<feature>
```

## Apply

- Add or alter routes through the owning feature route file when the feature already has one.
- Keep page-level orchestration in pages and reusable workflow pieces in feature components.
- Use shared layout/app wiring only when multiple features need the behavior.
- When introducing a new feature folder, mirror the smallest existing feature with similar needs.

## Verify

Use `yarn --cwd web build` for route/type integration. Add browser verification when navigation, guard behavior, or layout flow is user-critical.
