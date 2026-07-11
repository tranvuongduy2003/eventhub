# Form Workflow

Use this reference for react-hook-form, Zod schemas, and feature-specific server error mapping. Rules live in `web/AGENTS.md`; this file points to source patterns.

## Scout

Read the closest existing form:

- Auth forms: `web/src/features/auth/login-form.tsx`, `register-form.tsx`, and their error mappers.
- Event forms: `web/src/features/events/create-event-form.tsx`, `edit-event-form.tsx`, and related components.
- Shared schemas/types under `web/src/types` when values are reused.

Useful commands:

```powershell
rg "useForm|zodResolver|setError\('root'|FieldError|formState.errors" web/src
rg "ApiError|problem|errors" web/src/features web/src/types
```

## Apply

- Let the form own transient input state; let TanStack Query own server writes.
- Keep schemas close to the values they validate, moving them to `web/src/types` only when reused.
- Map backend errors into field errors or root errors at the feature boundary.
- Disable or guard duplicate submits for mutations that must not double-fire.
- Preserve existing UX language and server validation semantics from nearby flows.

## Verify

Run `yarn --cwd web lint` and `yarn --cwd web build`. Use browser verification when keyboard/focus, validation messages, duplicate submit prevention, or mobile layout matters.
