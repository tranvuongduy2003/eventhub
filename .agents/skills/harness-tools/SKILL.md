---
name: harness-tools
description: Create, update, and verify EventHub harness tool adapters and agent-friendly CLI contracts. Use when work touches harness/tools, MCP adapters, hosted tool adapters, local CLI wrappers, scripts used by skills, read/write command separation, JSON output contracts, or tool eval coverage.
---

# Harness Tools

Use this skill for the tool lane of the EventHub harness. Tools are execution surfaces; skills are workflow knowledge that may call them.

## Read First

Read the smallest relevant set:

1. `docs/_memory/source/harness-architecture.md`
2. `docs/_memory/source/harness-operational-policies.md`
3. `harness/tools/registry.json`
4. Existing `scripts/agent/*.ps1` when adding a local CLI surface
5. `harness/evals/README.md` when adding tool contract coverage

## Tool Contract

Every tool adapter or CLI must define:

- Purpose: the workflow it supports.
- Mode: read-only, write, or mixed.
- Inputs: arguments, stdin, environment variables.
- Outputs: JSON schema or concise text format.
- Side effects: files, network, services, or external systems touched.
- Approval: when user approval is required.
- Failure shape: exit codes and error messages.
- Eval: deterministic proof of the contract where possible.

## Workflow

1. Prefer existing scripts in `scripts/agent/` before adding a new tool.
2. Split read-only commands from side-effecting commands.
3. Prefer predictable JSON for agent-consumed output.
4. Keep hosted tool, MCP, and local CLI adapters separate in docs and code.
5. Add companion skill instructions when a tool has non-obvious sequencing.
6. Add eval coverage for parsing, allow/deny behavior, and important failure modes.

## Standard CLI Output

Prefer:

```json
{
  "status": "passed",
  "items": [],
  "errors": [],
  "timestamp": "2026-07-03T00:00:00Z"
}
```

Use nonzero exit codes for failed objective checks. Do not require the agent to scrape long prose.

## Validation

Run the tool's narrow command first, then:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Get-HarnessStatus.ps1 -Json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

## Do Not

- Hide write actions behind a read-looking command.
- Emit noisy logs when JSON is requested.
- Add production dependencies without explicit approval.
- Treat kubectl/goclaw-style examples as core harness components unless EventHub actually adopts those tools.
