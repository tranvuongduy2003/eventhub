---
name: verify-changed-code
description: Map the current diff to EventHub checks and run them. Use before handoff, after edits, or when a user asks whether the change is done.
---

# Verify Changed Code

Turn the working tree into objective verification evidence. This is the default answer to "is it done?"

## Commands

Plan checks without running them:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1 -PlanOnly
```

Run checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

Run for explicit paths:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1 -Path src/Domain/Users/User.cs
```

Use `-Json` for structured output.

## Behavior

- Reads changed, staged, and untracked files.
- Calls `node scripts/affected-tests.mjs <path>` for each relevant file.
- Deduplicates verification steps.
- Runs web typecheck when web TypeScript or JSX files changed.
- Uses the same backend/web commands as the hook harness.

## Rules

- Parent agent owns test interpretation and fixes.
- Do not call this a success unless the script exits 0.
- If `scripts/affected-tests.mjs` returns no scope for a meaningful source change, flag the coverage gap in the handoff.
- Do not edit generated code to satisfy checks.

## Output Contract

Summarize:

- commands planned or run
- pass/fail status
- error lines that explain the next fix

