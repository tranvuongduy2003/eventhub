---
name: frontend
description: "Source-driven frontend workflow for EventHub web/ work: React 19 + Vite + TypeScript routes, feature modules, auth/session UX, organizer/event/checkout/ticket/reporting workflows, OpenAPI client usage, TanStack Query, Zustand, Base UI/shadcn-style primitives, Tailwind v4 styling, and frontend verification. Use before editing or reviewing web/**."
---

# Frontend Skill

Use this skill to work from the current `web/` source code. Treat `AGENTS.md` files as rules/policy; keep this skill procedural and do not copy rules back into it.

## Boundary

- Rules live in root `AGENTS.md`, `web/AGENTS.md`, `web/src/generated/AGENTS.md`, and the three specs under `docs/`.
- This skill only tells Codex how to scout, edit, and verify frontend work.
- If a rule and this skill disagree, follow the rule/spec and report the drift in the skill.

## Frontend Entry Pass

1. Read root `AGENTS.md`, then `web/AGENTS.md`. Read `web/src/generated/AGENTS.md` only when generated API output is involved.
2. Read the relevant `docs/features.md` acceptance criteria for user-visible behavior. Add `docs/product.md` or `docs/technical.md` only when product guardrails, architecture, security, pricing, or integration behavior is at stake.
3. Discover the actual source before deciding an approach:

```powershell
rg --files web/src
rg "<domain term>|route|queryKey|mutationFn|schema|ProblemDetails|ApiError" web/src
```

4. Read nearby files in the owning feature before editing: route file, page, API module, components, schema/types, tests/e2e if present.
5. Choose only the reference files needed for the task.

## Source Map

Use the current codebase as the source of structure:

- `web/src/app`: router, providers, path constants, route guard composition.
- `web/src/features`: workflow-owned pages, API calls, local components, and feature-specific error mapping.
- `web/src/components/ui`: reusable primitives built around Base UI/shadcn conventions.
- `web/src/layouts`: shared page shells.
- `web/src/lib/api`: API client, interceptors, and OpenAPI type helpers.
- `web/src/hooks`: shared hooks.
- `web/src/store`: small client/session state stores.
- `web/src/styles`: Tailwind v4 globals and design-system utilities.
- `web/src/types`: reusable frontend schemas/types.

## Reference Routing

- Route, module, layout, or file placement changes: read `references/architecture.md`.
- UI primitive or component composition changes: read `references/components.md`; also use `$shadcn` when shadcn primitives or `components.json` are involved.
- API calls, server state, cache invalidation, or error surfaces: read `references/data-fetching.md`; use `$openapi-contract-sync` when API shape changes.
- Forms, validation, or server error mapping: read `references/forms.md`.
- Zustand or session/client state: read `references/state.md`; use `$zustand-web` for store design.
- Tailwind v4, tokens, responsive layout, or visual polish: read `references/styling.md`; use `$tailwind-patterns` for deeper Tailwind v4 work.
- Browser verification, screenshots, console, or network behavior: use `$chrome-devtools`/browser skill after implementation.

## Working Loop

1. Restate the frontend scope, touched feature/module, applicable rules/spec sections, and expected verification.
2. Build from existing source patterns instead of inventing a parallel pattern.
3. Keep edits local to the owning feature unless shared behavior is genuinely required.
4. Make user-facing states complete for the changed workflow: loading, empty, error, pending mutation, success/navigation, and mobile layout.
5. For contract drift, change the OpenAPI/backend contract through the owning workflow; do not patch generated frontend types by hand.
6. Before handoff, run the narrowest useful checks, then broaden when the change crosses shared app, API, or critical browser workflow boundaries.

## Source-Driven Checks

Before editing, answer from source:

- Which feature owns the workflow?
- Which existing route/page/API/helper already does the closest thing?
- Which generated contract type or local response type is currently used?
- Which cache keys, store fields, or navigation paths are affected?
- Which acceptance criteria or product guardrails does the user-visible behavior touch?

## References

- `references/architecture.md`
- `references/components.md`
- `references/data-fetching.md`
- `references/forms.md`
- `references/state.md`
- `references/styling.md`

## Verification

Pick commands from the actual change surface:

```powershell
yarn --cwd web api:verify
yarn --cwd web lint
yarn --cwd web format:check
yarn --cwd web build
```

Before handoff on changed-code work, prefer the repo verifier:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```
