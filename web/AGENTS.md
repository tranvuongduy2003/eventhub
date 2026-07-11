# EventHub web instructions

## Scope

Applies to `web/**`. Inherits the root instructions. Read `contracts/AGENTS.md` when a frontend change requires a public API shape change.

## Structure and data flow

- Keep router, providers, route paths, and guards under the existing `web/src/app` conventions.
- Keep feature-specific UI and data access inside `web/src/features`; reuse primitives from `web/src/components/ui`.
- Use generated API types from `web/src/generated`; never duplicate server contracts by hand.
- Use TanStack Query for server state.
- Use Zustand only for appropriate client/session/UI state, not as a second server-state cache.
- Keep route constants centralized in the existing paths module.
- Frontend guards improve navigation/UX only; backend authorization remains mandatory.

## Product guardrails

- Preserve all-inclusive pricing: the amount shown on event pages and checkout summaries must be the final amount charged.
- Optimize critical attendee journeys for phone-sized screens without horizontal scrolling or zooming.
- Keep guest checkout and public event access account-free where required by `features.md`.
- Make forms typed, accessible, keyboard-usable, validated, and clear about server/ProblemDetails errors.
- Use semantic HTML and existing accessible UI primitives; do not sacrifice QG-7 for visual convenience.
- Keep scope simple. Do not add generic state frameworks, design abstractions, or dependencies without demonstrated need and approval.

## Generated API output

- Do not edit `web/src/generated/**` directly.
- Change `contracts/openapi/api.v1.yaml`, update runtime contracts, and run the generation/verification workflow instead.
- Treat generated type drift as a contract issue, not a frontend typing issue to patch locally.

## Tooling and verification

Use Yarn, not npm.

```powershell
yarn --cwd web api:verify
yarn --cwd web build
```

Use Playwright only for critical browser journeys whose risk cannot be covered more cheaply at domain/application/API integration level.
