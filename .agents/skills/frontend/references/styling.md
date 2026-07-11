# Styling Workflow

Use this reference for Tailwind v4 classes, design tokens, responsive layout, and visual QA. Styling rules live in `web/AGENTS.md` and global design instructions; this file keeps the workflow source-driven.

## Scout

Read:

- `web/src/styles/globals.css` for tokens and Tailwind setup.
- `web/src/styles/design-system.css` for shared utilities.
- Nearby feature pages/components with similar density and responsive behavior.

Useful commands:

```powershell
rg "store-container|surface-|price-display|bg-|text-|grid|flex" web/src
rg "@theme|@layer|--color|@utility" web/src/styles
```

## Apply

- Start from existing tokens/utilities, then add narrowly scoped utilities only when repeated.
- Use responsive constraints for fixed-format UI such as ticket cards, summaries, filters, tables, and action bars.
- Check mobile widths early for attendee journeys and dense organizer workflows.
- Keep visual changes local to the feature unless they are part of the shared design system.

## Verify

Run `yarn --cwd web build`. Use browser screenshots for substantial visual changes, mobile-critical flows, sticky elements, tables, dialogs, and text-heavy cards.
