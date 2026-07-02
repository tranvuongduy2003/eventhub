---
name: pr-handoff
description: Create a reviewable summary for the current working tree. Use after verification passes or when preparing a PR/merge handoff.
---

# PR Handoff

Produce a concise review handoff from actual repo state.

## Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/New-PrHandoff.ps1
```

Use `-Json` when another tool needs structured output.

## Required Inputs

- Current git diff
- Verification evidence from `verify-changed-code`, `evals/run.ps1`, `dotnet test`, or web checks

## Rules

- Do not invent verification evidence.
- Include changed files and the checks that actually ran.
- Call out residual risk and reviewer focus.
- Do not commit, stage, push, or open a PR unless the user asks.

## Output Contract

Return:

- Summary
- Changed files
- Verification
- Risks and reviewer focus

