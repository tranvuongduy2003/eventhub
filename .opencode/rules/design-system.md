# DESIGN SYSTEM

Apply under `web/`. Consult `core.md`, `frontend.md`, [`docs/prd.md`](docs/prd.md) QG-4 first.

## Skills (read first)

`shadcn`, `tailwind-patterns`

## Principles

- **Simple and trustworthy** — clear pricing, obvious next steps (`prd.md` QG-2, QG-3)
- **Mobile-first** — public event pages and checkout must work on a phone (`features.md` F-4.2)
- **Latency felt** — skeletons within ~100ms; optimistic UI only when API contract allows
- **WCAG 2.1 AA floor** — labels, focus rings, contrast on forms and ticket QR display

## Stack

- Tailwind v4 `@theme` tokens in `styles/globals.css`
- shadcn primitives in `components/ui/` — add via CLI, customize in repo
- Icons: **lucide-react** only

## Key surfaces (by epic)

| Surface | Features | Notes |
|---------|----------|-------|
| Public event page | F-4.1, F-4.2 | Cover, schedule, ticket types with **final price**, buy CTA |
| Checkout | F-5.* | Guest name/email, line items, hold timer |
| Organizer dashboard | EP-2, EP-9 | Event list, draft/edit, attendee list |
| Ticket / QR view | F-7.3, F-7.4 | Large scannable QR on mobile |
| Check-in | F-8.* | Scanner + manual lookup, door counts |

Prices: show **all-inclusive** totals — never surprise fees (`DEC-1`).

## Tokens

Use semantic classes — **no raw hex in JSX**:

`bg-background`, `text-foreground`, `border-border`, `text-muted-foreground`, `bg-primary`, `text-destructive`

Status pills for order/ticket/event state: text + color (not color-only).

## Components

- Cards for event summaries; compact tables for attendee lists
- Loading: shadcn `Skeleton` / Query `isPending`
- Errors: inline `FormMessage` + RFC 7807 `code` mapped to human copy

## Accessibility checklist

- [ ] Every input has `<Label>` / `htmlFor`
- [ ] Icon-only buttons have `aria-label`
- [ ] Focus visible (`focus-visible:ring-2`)
- [ ] QR alt text / adjacent ticket code for screen readers where practical

## DON'TS

- ❌ No second component library
- ❌ No bespoke modal/dialog — shadcn primitives
- ❌ No hidden fee lines in checkout UI
- ❌ No emojis as primary affordances
