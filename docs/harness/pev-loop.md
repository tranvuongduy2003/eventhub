---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.pev-loop
title: Plan, Execute, Verify Loop
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Plan, Execute, Verify Loop

The harness uses a simple loop for repository work.

## 1. Plan

- Restate the desired outcome in testable terms.
- Identify affected layers: Domain, Application, Infrastructure, Api, web, e2e, contracts, or docs.
- Read nearby code before choosing an implementation shape.
- Decide the narrowest useful verification command.

## 2. Execute

- Keep edits scoped to the requested behavior.
- Follow existing naming, project structure, helpers, and test patterns.
- Update contracts and generated artifacts through their source workflow.
- Avoid unrelated refactors.

## 3. Verify

- Run focused checks first.
- Broaden when the change crosses layers or touches shared behavior.
- Record failures honestly and fix them when they are in scope.
- When a check cannot be run, state why and name the residual risk.

## Convergence Rules

- Passing tests are evidence, not proof. Compare the final behavior to the user's request.
- If implementation uncovers ambiguity, pause only for blocking questions.
- If verification reveals drift, update the implementation or tests before handoff.







