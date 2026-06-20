---
description: "Playwright e2e testing conventions for EventHub — folder structure, naming, fixtures, page objects, seed data, and TDD patterns. Use when writing or running tests in e2e/."
paths:
  - "e2e/**"
---

# E2E TESTING (Playwright)

Source: `CLAUDE.md`. Consult `frontend.md`, `api-guidelines.md`, `design-system.md` for UI selectors.

**No separate e2e skill** — conventions live here. See `playwright-e2e` skill for run/debug procedures.

## Stack

- **Playwright** with Chromium (MVP)
- **TypeScript** — strict mode, `NodeNext` module resolution
- **npm-free** — `e2e/` uses Yarn (consistent with `web/`)

## Prerequisites

- Aspire AppHost running (full stack: API + frontend + PostgreSQL + Redis + DataSeeder)
- Dev HTTPS cert trusted: `dotnet dev-certs https --trust`
- Chromium installed: `cd e2e && yarn install:browsers`

## Layout

```
e2e/
  fixtures/       Seed data loader, custom Playwright fixtures
  helpers/        Reusable auth/navigation helpers
  pages/          Page Object Models (selectors + actions)
  tests/
    auth/         Auth flow specs (register, login, logout, session, guards)
    <feature>/    Future: per-feature test folders
  playwright.config.ts
  package.json
  tsconfig.json
```

## Naming

- Test files: `<feature>.spec.ts` (e.g., `login.spec.ts`)
- Test names: `<action> <expected outcome>` (e.g., "seed user logs in and sees dashboard")
- Page objects: `<page>.page.ts` (e.g., `login.page.ts`)
- Fixtures: `<name>.fixture.ts` or plain `.ts`

## Seed data

All seed data comes from `src/DataSeeder/Data/*.json` via `fixtures/seed-data.ts`:

| Export | Source | Usage |
|--------|--------|-------|
| `users` | `Users.json` | All 90 seed users |
| `organizers` | filtered | 30 Organizer users |
| `attendees` | filtered | 60 Attendee users |
| `alice` | `organizers[0]` | Default test organizer |
| `dave` | `attendees[0]` | Default test attendee |
| `SEED_PASSWORD` | `"DevPass123!"` | Shared password for all seed users |

**Do not create new users in tests that depend on seed data existing.** Registration tests use unique timestamps to avoid conflicts.

## Page Objects

Use the Page Object Model pattern:

```typescript
import { LoginPage } from "../pages/login.page";

test("logs in", async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.login("alice@eventhub.dev", "DevPass123!");
});
```

- Locators use **semantic selectors**: `getByRole`, `getByText`, `#id` from the React forms
- No `data-testid` unless the element has no accessible selector
- Page objects expose actions, not raw locators

## Custom fixtures

`fixtures/auth.fixture.ts` extends Playwright's `test` with:

| Fixture | Description |
|---------|-------------|
| `authenticatedPage` | Page already logged in as default seed user (Alice) |
| `seedUser` | The `SeedUser` used for auth (overridable) |

```typescript
import { test, expect } from "../fixtures/auth.fixture";

test("dashboard shows welcome", async ({ authenticatedPage: page }) => {
  await expect(page.getByRole("heading")).toContainText("Welcome back");
});
```

## TDD workflow

1. Write a failing test (red)
2. Implement the feature
3. Run test until green
4. Refactor

For e2e, "red" means the test fails against the current frontend. Tests should be written **before** the UI feature, then the frontend is built to make them pass.

## Selector strategy

Prefer in order:
1. ARIA roles: `getByRole("button", { name: "Log in" })`
2. Form labels: `getByLabel("Email")` or `#login-email`
3. Text content: `getByText("Welcome back")`
4. CSS selectors: `locator('[data-slot="alert"]')` — last resort

## What NOT to test via e2e

- Unit-level domain logic (use `Domain.UnitTests`)
- API contract validation (use `Api.IntegrationTests`)
- Visual regression (out of MVP scope)
- Third-party auth providers (none in EventHub)

## Code style

- **No comments** — no JSDoc, no inline comments, no section headers. Code should be self-explanatory through naming and structure.

## DON'TS

- No `page.waitForTimeout()` — use `waitForURL`, `waitFor`, or Playwright auto-waiting
- No hardcoded URLs — use `baseURL` from config
- No inter-test dependencies — each spec is independent
- No `npm` — use `yarn` in `e2e/`
- No tests against placeholder pages ("Coming soon") — only implemented features
- No comments in code (JSDoc, inline, or section headers)
