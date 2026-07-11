# Source Discovery

Read `e2e/AGENTS.md` first for durable E2E rules. Use this reference to orient from the current source tree before editing.

## Commands

```powershell
rg --files e2e
rg -n "test\\(|test\\.describe|test\\.skip|test\\.only|waitForTimeout" e2e
rg -n "class .*Page|authenticatedPage|loginAs|registerUser|E2E_BASE_URL|E2E_API_URL" e2e
```

## Reading Order

1. `e2e/playwright.config.ts`
2. Nearby specs under `e2e/tests/**`
3. Page objects under `e2e/pages/**`
4. Fixtures/helpers under `e2e/fixtures/**` and `e2e/helpers/**`
5. Data seeding source used by the fixture, when relevant

## Scenario Note

Before editing, write down the target behavior in terms of source truth: feature id, journey, required setup, browser-visible result, and command to verify it. Keep that note in the working plan or final handoff, not as committed documentation unless the task explicitly asks for docs.
