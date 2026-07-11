---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.index
title: Codex Harness
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Codex Harness

This harness describes how Codex should plan, implement, verify, and review work in this repository.

## Core Files

- `AGENTS.md`: standing repository instructions.
- `.agents/skills/`: reusable workflows for backend, web, E2E, OpenAPI, and harness work.
- `.codex/agents/`: role prompts for focused review or implementation passes.
- `.codex/hooks.json`: hook registration.
- `.codex/hooks/`: deterministic guard, formatter, and stop verification scripts.
- `scripts/agent/Test-HarnessPolicy.ps1`: regression sensor for guardrails and operating behavior.

## Operating Loop

1. Clarify the requested outcome and acceptance criteria.
2. Inspect nearby code and tests.
3. Plan the smallest coherent slice.
4. Implement through the owning layer.
5. Verify with focused checks, then broaden according to risk.
6. Report changed files, evidence, and remaining risk.

See:

- `pev-loop.md`
- `permission-tiers.md`
- `evidence-bundle.md`
- `repository-view.md`
- `shared-substrate.md`
- `caveats.md`






