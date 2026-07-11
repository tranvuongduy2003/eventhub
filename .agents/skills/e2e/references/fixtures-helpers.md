# Fixtures and Helpers

Read `e2e/AGENTS.md` first for durable E2E rules. This reference is only about fixture/helper workflow.

## Procedure

- Inspect existing support code before adding new setup: `rg --files e2e/fixtures e2e/helpers`
- Prefer extending the existing fixture when a setup pattern is shared across specs.
- Prefer a helper when the behavior is an action that can be called from multiple fixtures or specs.
- Keep source-of-truth seed data aligned with the files loaded by `e2e/fixtures/seed-data.ts`.
- If a scenario uses backend setup or cleanup, make the API base URL and session assumptions explicit in the helper or fixture.
