# State Workflow

Use this reference when touching client/session state. The policy boundary lives in `web/AGENTS.md`; this file is for source-driven store edits.

## Scout

Read current stores and their call sites:

```powershell
rg --files web/src/store
rg "use.*Store|create\(" web/src
```

## Apply

- Keep store state minimal and explicit.
- Prefer selectors at call sites when a component only needs one or two fields/actions.
- Keep session transitions coordinated with TanStack Query cache clearing/fetching.
- Add a new store only when local component state, URL state, or TanStack Query is the wrong ownership model.

## Verify

Run `yarn --cwd web build`. Browser-check login/logout/session flows when auth state or route guards are involved.
