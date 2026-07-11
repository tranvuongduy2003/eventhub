# EventHub end-to-end testing instructions

## Scope

Applies to `e2e/**`. Inherits the root instructions.

## Purpose

Use Playwright for critical user journeys whose browser/UI integration risk cannot be covered more cheaply by domain, application, or API integration tests. The primary end-to-end spine is create/publish -> select tickets -> guest checkout/payment or free confirmation -> ticket access -> check-in -> organizer results.

## Skill boundary

This file owns durable rules and constraints for `e2e/**`. The repo-local `e2e` skill owns the procedural workflow for discovering the current suite, choosing patterns, and running verification; it must link back here instead of restating these rules.

## Structure

- Keep scenarios under the existing feature-oriented test folders.
- Put repeated UI interactions and selectors in page objects.
- Put fictional seed/build data in fixtures and reusable behavior in helpers.
- Reuse the existing API setup/cleanup path rather than coupling every test to slow UI setup.

## Rules

- Prefer role, label, and test-id selectors that express user intent; avoid brittle DOM/CSS chains.
- Avoid fixed sleeps. Wait for observable UI, network, or state transitions.
- Keep tests independent, deterministic, and safe to run in any order.
- Do not commit `test.only`, skipped critical tests, screenshots/videos containing sensitive data, or `.env` files.
- Use only fictional names, emails, tickets, payment data, and events.
- Assert the behavior in `features.md`, including public/guest access, mobile usability where relevant, stable error states, authorization boundaries, duplicate-scan behavior, and transparent totals.
- Keep concurrency correctness primarily in integration tests; use browser concurrency only when the UI/device interaction itself is the risk.

## Verification

```powershell
yarn --cwd e2e test
```
