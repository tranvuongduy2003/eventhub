---
name: e2e
description: "Source-driven workflow for EventHub Playwright E2E work under e2e/: discover current browser tests, page objects, fixtures, auth/session setup, Aspire-backed runtime behavior, and verification for critical user journeys such as auth, event creation/publishing, checkout, tickets, check-in, and organizer results. Use when adding, changing, reviewing, or debugging e2e/** tests."
---

# E2E Skill

Use this skill for Playwright work under `e2e/**`.

## Boundary

`e2e/AGENTS.md` owns durable rules for `e2e/**`. Do not duplicate those rules here or in references. This skill owns the working procedure: what to read, how to choose existing patterns, and which checks to run.

## Start Here

1. Read `e2e/AGENTS.md`.
2. Read the relevant source spec slice:
   - `docs/features.md` for the `F-*` acceptance criteria under test.
   - `docs/technical.md` section 17 for verification strategy and mandatory risk scenarios.
   - `docs/product.md` only when the test touches product guardrails such as mobile, transparency, fairness, or responsible data/money handling.
3. Discover the current suite from source instead of relying on stale lists:
   - `rg --files e2e`
   - `rg -n "test\\(|test\\.describe|test\\.skip|test\\.only|waitForTimeout|page\\.request|storageState|webServer|baseURL" e2e`
4. Read the nearest existing tests, page objects, fixtures, helpers, and `e2e/playwright.config.ts` before editing.

## Workflow

1. Choose E2E only when browser integration risk is the point. Prefer lower-level tests for domain invariants, API contracts, persistence, concurrency, authorization mechanics, and idempotency when the browser adds no evidence.
2. Map the scenario to source truth before coding: note the `F-*` behavior, user journey, setup data, expected observable outcome, and verification command.
3. Reuse the current suite shape. Add or extend page objects/helpers when an interaction repeats or represents a workflow concept; keep one-off journey assertions in the spec.
4. Keep setup explicit and cheap. Prefer seeded identities/data and API setup/cleanup paths already present in fixtures/helpers when UI setup is not the behavior under test.
5. For bugs, make the failing behavior visible in a focused spec first when feasible, then implement or adjust the test support code.
6. Run the narrowest affected Playwright command first, then broaden if the change touches shared fixtures, config, or critical paths.

## References

- Read `references/source-discovery.md` when orienting in the current suite.
- Read `references/page-objects.md` when adding or changing page objects.
- Read `references/fixtures-helpers.md` when changing auth/session setup, seed data, or reusable helpers.
- Read `references/running.md` before running or debugging Playwright.

## Verification

```powershell
yarn --cwd e2e test
```

Use narrower commands while iterating, for example:

```powershell
yarn --cwd e2e playwright test tests/auth/login.spec.ts
```
