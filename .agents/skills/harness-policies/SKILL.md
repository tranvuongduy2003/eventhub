---
name: harness-policies
description: Create, update, and verify EventHub harness policies and guardrails. Use when work touches .codex/policies/harness-policy.json, harness/policies, protected paths, dangerous command rules, approval policy, permission mapping, hook enforcement data, or policy eval coverage.
---

# Harness Policies

Use this skill for the policy lane of the EventHub harness. Policy data lives in `.codex/policies/` today; future runtime policy contracts may live under `harness/policies/`.

## Read First

Read the smallest relevant set:

1. `docs/_memory/source/harness-operational-policies.md`
2. `docs/_memory/source/harness-architecture.md`
3. `harness/policies/runtime-policy.json`
4. `.codex/policies/harness-policy.json`
5. `.codex/hooks/lib/Use-GuardRules.ps1`
6. `scripts/agent/Test-HarnessPolicy.ps1`

## Policy Change Format

Every policy change must record:

- Rule: what is allowed, denied, or gated.
- Scope: paths, commands, tools, or workflow phase.
- Enforcement: hook, script, runtime policy, or documentation-only.
- Failure message: what the agent or user sees when blocked.
- Eval: deterministic case or test assertion that proves the rule.
- Escape hatch: whether user approval can override it.

## Workflow

1. Change policy data before hook code when the behavior can be data-driven.
2. Keep hooks thin; move reusable command logic to `.codex/hooks/lib/` or `scripts/agent/`.
3. Add a positive and negative assertion when the policy affects allow/deny behavior.
4. Update `scripts/agent/Test-HarnessPolicy.ps1` for static policy invariants.
5. Run harness evals.

## Validation

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1 -Json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Get-HarnessStatus.ps1 -Json
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/Invoke-HarnessEvals.ps1 -Layer harness
```

If path-to-check routing changed, also run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/Invoke-HarnessEvals.ps1 -Layer graph
```

## Do Not

- Put durable policy in `.codex/state/`.
- Hard-code broad policy in a hook when `.codex/policies/harness-policy.json` can own it.
- Weaken protected paths for convenience.
- Use policy changes to bypass Constitution or AGENTS.md rules.
