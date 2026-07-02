---
title: Source Of Truth Map
type: memory_index
status: active
tags:
  - memory/index
  - source-of-truth
---

# Source Of Truth Map

## Precedence

1. [[CONSTITUTION]] - non-negotiable repository invariants.
2. [[_memory/source/product-requirements]], [[_memory/source/feature-specification]], [[_memory/source/domain-model-specification]], [[_memory/source/technical-design]] - product, domain, and implementation contract.
3. [[_memory/source/harness-architecture]], [[_memory/source/harness-operational-policies]] - Codex harness contract.
4. Scoped rules, skills, implementation plans, and memory notes.

If lower-level material drifts, fix the lower-level material. Do not weaken the higher-level source.

## Documents

| Document | Use for | Memory entry |
|---|---|---|
| [[CONSTITUTION]] | Immutable architecture, data, API, local run, naming, testing | [[_memory/glossary/architecture-invariants]] |
| [[_memory/source/product-requirements]] | Product intent, personas, goals, quality guardrails, `DEC-*` decisions | [[_memory/mocs/product-intent]], [[_memory/glossary/decision-log]] |
| [[_memory/source/feature-specification]] | Epics, features, phases, dependencies, acceptance criteria | [[_memory/mocs/feature-roadmap]] |
| [[_memory/source/domain-model-specification]] | Bounded contexts, aggregates, invariants, events, context map | [[_memory/mocs/domain-model]], [[_memory/glossary/ubiquitous-language]] |
| [[_memory/source/technical-design]] | Layer layout, CQRS, infrastructure, persistence, API, testing | [[_memory/mocs/technical-architecture]] |
| [[_memory/source/harness-architecture]] | Agent harness definition and layers | [[_memory/mocs/harness-memory]] |
| [[_memory/source/harness-operational-policies]] | Hooks, verification, state, policy, improvement loop | [[_memory/mocs/harness-memory]] |
| `docs/_memory/specs/` | Implementation-ready feature specs | [[_memory/mocs/feature-roadmap]] |

## Reading by task type

| Task | First read | Then read |
|---|---|---|
| Product scope or UX behavior | [[_memory/source/product-requirements]] | [[_memory/source/feature-specification]], relevant spec in `docs/_memory/specs/` |
| Domain rule or invariant | [[_memory/source/domain-model-specification]] | [[_memory/source/feature-specification]], relevant spec, [[_memory/source/technical-design]] for mechanics |
| API or persistence change | [[CONSTITUTION]], [[_memory/source/technical-design]] | Relevant spec, [[_memory/source/domain-model-specification]], OpenAPI contract |
| Harness or agent workflow | [[_memory/source/harness-architecture]] | [[_memory/source/harness-operational-policies]], `AGENTS.md`, relevant skill |
| Verification or testing | [[CONSTITUTION]], [[_memory/source/technical-design]] | `.graph/index.json`, relevant skill |
