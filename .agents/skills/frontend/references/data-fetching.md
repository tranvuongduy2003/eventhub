# Data Fetching Workflow

Use this reference for API calls, TanStack Query usage, mutation flows, cache invalidation, and ProblemDetails display. Contract rules live in `web/AGENTS.md`, `contracts/AGENTS.md`, and OpenAPI workflow skills.

## Scout

Read:

- `web/src/lib/api/client.ts`, `interceptors.ts`, `openapi.ts`, and `types.ts`.
- The owning feature's `api.ts` and pages/components using `useQuery` or `useMutation`.
- Auth/session query clearing when login/logout or user-scoped data is involved.

Useful commands:

```powershell
rg "useQuery|useMutation|queryKey|invalidateQueries|clearUserScopedQueries|ApiError" web/src
rg "apiClient\.|ApiJsonResponse|ApiJsonBody" web/src
```

## Apply

- Keep endpoint wrappers in the owning feature's API module unless the call is shared.
- Keep request serialization, credentials/session headers, and global response handling in shared API plumbing.
- Keep user-facing error mapping near the feature UI that renders it.
- Use stable query keys and await invalidations when navigation or subsequent reads depend on refreshed data.
- If the API shape is missing or stale, switch to the OpenAPI/backend workflow instead of locally papering over types.

## Verify

Run `yarn --cwd web api:verify` when API contract usage is involved, then `yarn --cwd web build`. Use browser/network verification for auth, checkout, payment redirects, or high-risk mutation flows.
