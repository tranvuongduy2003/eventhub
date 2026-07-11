---
name: chrome-devtools
description: >
  Entry point for the chrome-devtools MCP server - drive and inspect a real Chrome
  browser to verify UI behavior, debug console errors and network requests, record
  performance traces, and run Lighthouse audits. Use when you need to confirm a
  web change actually works in a browser (not just tests), reproduce a UI bug,
  read console errors or network requests for a page, screenshot a page, or check
  page performance. Triggers on: "browser automation", "inspect page", "console
  errors", "network requests", "performance trace", "lighthouse", "verify UI in a
  real browser", "screenshot a page", "click / fill / navigate the app", "reproduce
  in the browser".
---

# Chrome DevTools MCP

This skill is the entry point for the **`chrome-devtools`** MCP server (configured in
`.mcp.json`, launched as `chrome-devtools-mcp@latest --autoConnect --channel=stable`).
Its tools are prefixed `mcp__chrome-devtools__*` and control a live Chrome instance via
Puppeteer + the Chrome DevTools Protocol. The exact tool list is discovered at runtime
from the MCP server; the categories below reflect the official server.

## When to use

- **Verify a change in a real browser** - drive the affected flow and observe the result, rather than trusting a passing test alone. This is the browser backend for the `verify` / acceptance-verifier flows.
- **Debug console errors** - read runtime errors and warnings with source-mapped stack traces.
- **Inspect network requests** - see requests a page made, their status, headers, timing, and bodies (e.g. confirm an `/api` call fired with the expected payload/response).
- **Reproduce and confirm UI bugs** - navigate, interact, and screenshot to see the actual rendered state.
- **Performance** - record a trace and get actionable insights, or run a Lighthouse audit.

Prefer this over guessing about browser behavior. Do NOT use it for backend-only or non-UI changes - there is nothing to observe in a browser.

## Tool categories

| Category | Tools | Use for |
|----------|-------|---------|
| **Navigation** | `navigate_page`, `new_page`, `list_pages`, `select_page`, `close_page`, `wait_for` | Open/switch tabs, go to a URL, wait for expected text/state |
| **Snapshots & screenshots** | `take_snapshot`, `take_screenshot` | Get a structured accessibility snapshot with element **uids** (before interacting); capture a visual image |
| **Input automation** | `click`, `fill`, `fill_form`, `hover`, `drag`, `type_text`, `press_key`, `upload_file`, `handle_dialog` | Drive the page - all element targeting is by the **uid** from the latest snapshot |
| **Console & network** | `list_console_messages`, `get_console_message`, `list_network_requests`, `get_network_request` | Read console output and network activity to debug |
| **Scripting** | `evaluate_script` | Run JavaScript in the page to read/assert DOM or app state |
| **Emulation** | `emulate`, `resize_page` | Emulate device/network/CPU conditions; set viewport size |
| **Performance** | `performance_start_trace`, `performance_stop_trace`, `performance_analyze_insight` | Record a trace and extract insights |
| **Audit** | `lighthouse_audit` | Full Lighthouse assessment of a page |
| **Memory** | `take_heapsnapshot` | Capture a heap snapshot for memory investigation |

## Recommended workflow

1. **Navigate** - `navigate_page` (or `new_page`) to the target URL.
2. **Snapshot first** - call `take_snapshot` to get the current elements and their **uids**. You MUST have a fresh snapshot uid before any interaction; the DOM changes after every action, so re-snapshot when the page updates.
3. **Interact by uid** - pass the uid to `click` / `fill` / `fill_form` / `hover`. Puppeteer auto-waits for the action to settle.
4. **Wait for state, not time** - use `wait_for` (expected text/condition) instead of fixed sleeps.
5. **Observe** - `take_screenshot` for a visual check, `list_console_messages` for errors, `list_network_requests` / `get_network_request` to confirm API calls, or `evaluate_script` to assert app state.
6. **Performance (optional)** - `performance_start_trace`, exercise the flow, `performance_stop_trace`, then `performance_analyze_insight`; or run `lighthouse_audit` for a one-shot audit.

Guidelines:
- Snapshot uids are the reliable way to target elements - do not hand-craft selectors when a snapshot uid exists.
- Re-take a snapshot after navigation or any action that mutates the DOM; stale uids fail.
- Keep interactions minimal and driven by what the snapshot actually shows.

## How it fits this project

- The web dev server runs on **port 5000** and proxies `/api` and `/api` to the API URL from VITE_API_URL (see `web/AGENTS.md`). Start the local stack with `$aspire-mcp` (or `powershell -NoProfile -ExecutionPolicy Bypass -File dotnet run --project src/AppHost/EventHub.AppHost.csproj`), then point `navigate_page` at `http://localhost:5000`.
- Use this to confirm a web change renders and behaves correctly end-to-end, to read console/network detail while debugging, and as the browser-driving backend for the `verify` acceptance gate.
- User data (names, sensitive identifiers) must never be copied into logs, comments, or persistent files - the browser exposes real data, so treat screenshots and network bodies as sensitive (sensitive data policy).
- For scripted, committed browser tests use Playwright and the `e2e` skill instead; this MCP server is for interactive verification and debugging during development.








