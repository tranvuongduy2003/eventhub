# Page Objects

Read `e2e/AGENTS.md` first for durable E2E rules. This reference is only about local page-object workflow.

## Procedure

- Inspect nearby page objects before adding a new one: `rg --files e2e/pages`
- Match the current class-per-screen style and constructor-injected `Page` pattern.
- Add locators as named readonly fields when reused by more than one method or assertion.
- Add methods for repeated user actions and navigation.
- Leave journey-specific assertions in specs unless multiple specs need the same assertion helper.
