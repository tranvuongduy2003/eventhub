# Component Workflow

Use this reference when composing pages or primitives. Component rules live in `web/AGENTS.md` and the design instructions; this file helps find and extend existing source patterns.

## Scout

Read:

- `web/src/components/ui` for available primitives and variants.
- Nearby feature components for composition style.
- `web/components.json` when shadcn/Base UI conventions matter.

Useful commands:

```powershell
rg "from '@/components/ui|lucide-react|data-invalid|aria-" web/src
rg --files web/src/components/ui
```

## Apply

- Compose from existing primitives first; add a primitive only when repeated usage or accessibility complexity justifies it.
- Keep feature-specific components inside the feature unless they are genuinely shared.
- Put interaction state and data fetching near the feature workflow; keep primitives mostly presentational.
- Preserve accessible names, labels, disabled/pending states, and keyboard behavior from nearby examples.

## Verify

Run `yarn --cwd web lint` and `yarn --cwd web build`. Use browser verification for layout, focus, menus/dialogs, responsive behavior, or any interaction that TypeScript cannot prove.
