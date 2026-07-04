---
title: Harness Memory MOC
type: moc
status: active
tags:
  - moc/harness
  - harness
  - memory
---

# Harness Memory MOC

Authoritative sources: [[_memory/source/harness-architecture|harness architecture]], [[_memory/source/harness-operational-policies|harness operational policies]], and root [`AGENTS.md`](../../../AGENTS.md).

## Harness definition

Harness is the contract around an agent run: repository guidance, skills and scripts, policies, hooks, state and memory artifacts, verification, evals, and future orchestration runtime.

It is not a pile of prompt examples or product implementation code.

## Current layers

| Layer | Location |
|---|---|
| Repo guidance | `AGENTS.md` |
| Runtime manifest | `harness/manifest.json` |
| Skills | `.agents/skills/` |
| Execution scripts | `scripts/agent/` |
| Policy data | `.codex/policies/harness-policy.json` |
| Lifecycle hooks | `.codex/hooks/` |
| Verification graph | `.graph/index.json` |
| Runtime state | `.codex/state/` |
| Evals | `harness/evals/` |
| Runtime scaffold | `harness/` |
| Long-term docs memory checks | `scripts/agent/Test-DocsMemory.ps1` |

## Memory split

- Working memory: active conversation context.
- Task memory: `.codex/plans/`, `.codex/notes/`, eval outputs, handoff text.
- Long-term knowledge memory: this Obsidian vault under `docs/`, validated by `scripts/agent/Test-DocsMemory.ps1` and routed by `.graph/index.json`.

## Workflow memory sync

The `cook-unified` path is the single feature-delivery workflow. Inside `cook`, the phases `spec` -> `plan` -> checkpoint implementation -> verify -> memory sync -> handoff must keep long-term docs memory current: new durable specs are discoverable from the relevant indexes, plans include a `memory-sync` inventory, and cook marks completed specs implemented only after checks pass while refreshing every affected source doc, MOC, glossary, retrieval guide, README/index, harness contract, graph/routing entry, and handoff evidence surface.

Cook dry-run/audit mode is read-only for product artifacts: it reports intended specs/plans/checks and adjacent-feature risks without creating durable specs, plans, progress notes, code changes, or memory updates.

Cook plans are enforceable artifacts: feature-id runs include an Adjacent Feature Boundary, every plan includes a Done Criteria Ledger, long runs maintain `.codex/notes/progress.md`, and `scripts/agent/Test-CookPlan.ps1` validates plan/progress/TaskSpec shape before implementation starts.

## Improvement loop

When the harness fails:

1. Capture the failure as a command, hook fixture, eval case, or concise note.
2. Change the smallest owning layer.
3. Run the relevant eval or verification script.
4. Promote only durable lessons into long-term memory.

## Future runtime guardrail

If an application-owned orchestrator is built, use Responses API and Agents SDK. Do not start new runtime design on Assistants API.
