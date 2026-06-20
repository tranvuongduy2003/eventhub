---
name: playwright-e2e
description: Run, debug, and author Playwright e2e tests against the EventHub stack.
---

# Playwright E2E Skill

Procedures for running and debugging Playwright end-to-end tests.

## Prerequisites

1. Aspire AppHost running (`src/AppHost/`)
2. HTTPS dev cert trusted: `dotnet dev-certs https --trust`
3. Browsers installed: `cd e2e && yarn install:browsers`

## Run all tests

```bash
cd e2e && yarn test
```

## Run specific test file

```bash
cd e2e && yarn test tests/auth/login.spec.ts
```

## Run with UI mode (interactive)

```bash
cd e2e && yarn test:ui
```

## Debug a test

```bash
cd e2e && yarn test:debug tests/auth/login.spec.ts
```

Opens Playwright Inspector — step through actions, inspect locators.

## View HTML report

```bash
cd e2e && yarn report
```

## Run headed (see browser)

```bash
cd e2e && yarn test --headed
```

## Run with specific grep

```bash
cd e2e && yarn test --grep "login"
```

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `E2E_BASE_URL` | `https://localhost:5000` | Frontend base URL |
| `CI` | unset | Set to enable retries, serial workers |

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `net::ERR_CERT_AUTHORITY_INVALID` | Run `dotnet dev-certs https --trust` |
| `ECONNREFUSED` on all tests | Aspire stack not running — start AppHost |
| `Timeout waiting for navigation` | Check if API is healthy: `curl -k https://localhost:<api-port>/api/health` |
| Seed user login fails | DataSeeder may not have run — check Aspire dashboard |

## Agent usage

When an agent needs to run e2e tests:

```bash
cd c:/Users/duyvt/eventhub/e2e && yarn test --reporter=line
```

Use `--reporter=line` for compact output in agent context. Use `--grep` to run specific tests.

## Adding new tests

1. Create `e2e/tests/<feature>/<name>.spec.ts`
2. Import from `../../fixtures/auth.fixture` for authenticated tests
3. Use page objects from `../../pages/`
4. Follow `Method_Scenario_Expected` naming (adapted: `Action_Scenario_Expected`)
