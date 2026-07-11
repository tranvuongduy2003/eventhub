# Running E2E Tests

Read `e2e/playwright.config.ts` before assuming how the browser suite starts the app. The current suite is Aspire-backed through Playwright `webServer` configuration and uses `E2E_BASE_URL` / `E2E_API_URL` when supplied.

## Install

```powershell
yarn --cwd e2e install
yarn --cwd e2e install:browsers
```

## Run

```powershell
yarn --cwd e2e test
```

Narrow examples:

```powershell
yarn --cwd e2e playwright test tests/auth/login.spec.ts
yarn --cwd e2e playwright test tests/events
yarn --cwd e2e playwright test --debug
```

If the server is already running, Playwright should reuse it according to config. If startup fails, inspect the AppHost/API/web output rather than bypassing the Aspire topology.
