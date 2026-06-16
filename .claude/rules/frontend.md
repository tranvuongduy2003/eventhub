---
description: "React 19 + TypeScript frontend conventions — TanStack Query for server state, Zustand for client state, react-hook-form + zod forms, Yarn-only package management, feature folder structure, and SignalR realtime. Use when writing code in web/src/."
paths:
  - "web/**/*.ts"
  - "web/**/*.tsx"
  - "web/**/*.css"
  - "web/**/package.json"
  - "web/**/vite.config.*"
---

# FRONTEND (web/)

Source: [`docs/features.md`](../../docs/features.md), [`docs/prd.md`](../../docs/prd.md) (QG-4 mobile), [`docs/technical.md`](../../docs/technical.md) §7. See `design-system.md`, `api-guidelines.md`. Consult `CLAUDE.md` first.

## Skills (read first)

| Area | Skill |
|------|-------|
| Server state | `tanstack-query` |
| UI components / styling | `shadcn`, `tailwind-patterns` |

## Stack

React 19, TypeScript, Vite, Tailwind v4, shadcn/ui, **TanStack Query**, **Zustand**, react-hook-form + zod.

## Package manager (Yarn)

- **`web/` uses Yarn only** — lockfile is `yarn.lock`. Do not run `npm install` or commit `package-lock.json`.
- Prefer commands from repo root: `yarn --cwd web install`, `yarn --cwd web dev`, `yarn --cwd web build`, `yarn --cwd web lint`.
- shadcn: `yarn --cwd web dlx shadcn@latest add <component>` (or `yarn --cwd web exec shadcn add <component>`).
- Aspire AppHost: `AddViteApp("web", ...).WithYarn(...)` on the `web` resource (Aspire 13.3+).

## API access

- Frontends call **Api directly** — cookie session for organizers; guest flows without account where `features.md` allows
- Base URL from Vite env (`VITE_*` injected by Aspire)
- Centralize HTTP in `web/src/lib/` — feature code does not hardcode URLs

## Server state (TanStack Query)

```tsx
const sessionQuery = useQuery({
  queryKey: ['auth', 'session'],
  queryFn: ({ signal }) => authApi.getCurrentUser({ signal }),
  staleTime: 60_000,
});
```

- All REST reads/writes through Query mutations
- Mutations: invalidate or set query data on success

## Client state (Zustand)

- UI-only ephemeral state
- **Never** duplicate server session data in Zustand beyond the auth shell (`useAuthStore`)

## Folder structure

```
web/src/
  app/           providers, router
  features/      auth | events | checkout | tickets | check-in | …
  components/    shared + ui/ (shadcn)
  lib/           api client, utils
  store/         zustand
  types/         zod schemas
```

One feature = one folder. Cross-feature imports via `components/` or `lib/` only.

## Forms

react-hook-form + zod in `types/` — same schema for inference and validation.

## Realtime (EP-11)

SignalR for live sales and check-in counts. Subscribe in feature hooks; invalidate Query cache on events.

## Testing

No automated frontend tests for MVP unless explicitly requested.

## DON'TS

- No `useEffect` for data fetching — use TanStack Query
- No hex colors in JSX — Tailwind semantic tokens (`bg-primary`, `text-muted-foreground`)
- No `dangerouslySetInnerHTML` for API-sourced strings
- No global mutable singletons outside Zustand/React context
