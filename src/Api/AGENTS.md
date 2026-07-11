# EventHub API instructions

## Scope

Applies to `src/Api/**`. Inherits `src/AGENTS.md` and the root instructions. Contract changes also require reading `src/Contracts/AGENTS.md`, `contracts/AGENTS.md`, and `web/AGENTS.md`.

## Thin transport

- Minimal endpoints implement the existing `IEndpoint` convention and are discovered through assembly scanning.
- Endpoints bind, authenticate/authorize, dispatch to Application, and map results to HTTP only.
- Do not implement business invariants, persistence orchestration, or provider-specific business logic in endpoints/hubs.
- Return explicit Contracts DTOs, never domain entities or EF models.
- Reuse existing result-to-response and ProblemDetails helpers.

## HTTP behavior

- Keep endpoint contracts explicit and aligned with `contracts/openapi/api.v1.yaml`.
- Malformed JSON/binding failures return 400; semantic validation failures return 422.
- Keep RFC 7807 ProblemDetails error codes stable and intentional.
- Preserve unauthenticated public event viewing and guest checkout where required by `features.md`.
- Perform event-scoped authorization for protected operations; do not rely on route visibility.

## Security boundaries

- Browser auth uses secure, HttpOnly, SameSite-appropriate cookies and server-side session validation through centralized helpers.
- Payment webhooks validate provider signatures before trusting/parsing outcome data and must be idempotent.
- Ticket links/codes are unguessable and expose only the minimum necessary data.
- SignalR uses the same authenticated session and authorization model as REST.
- Never log passwords, cookies, session secrets, credentials, raw payment secrets, full ticket codes, or sensitive uploaded content.

## Contract workflow

For a public REST shape change:

1. reconcile changed behavior with the relevant `F-*` acceptance criteria;
2. update the committed OpenAPI source under `contracts/`;
3. update Contracts and endpoint mapping;
4. regenerate/verify web API types through the repository workflow;
5. add or update API integration tests for status codes, ProblemDetails, auth/session behavior, and persistence effects.

## Verification

```powershell
yarn --cwd web api:verify
dotnet test EventHub.slnx -c Release
```
