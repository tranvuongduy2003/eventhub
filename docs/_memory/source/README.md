---
title: Authoritative Source Memory
type: source_index
status: active
tags:
  - memory/source
  - source-of-truth
---

# Authoritative Source Memory

This folder holds the durable source documents that used to live as loose top-level docs. They are now first-class Obsidian knowledge memory.

## Documents

| Source | Owns |
|---|---|
| [[product-requirements]] | Product intent, personas, scope, quality guardrails, decisions, assumptions, dependencies |
| [[feature-specification]] | Epics, feature IDs, phases, dependencies, acceptance criteria, build order |
| [[domain-model-specification]] | Bounded contexts, aggregates, invariants, value objects, domain and integration events |
| [[technical-design]] | Clean Architecture, CQRS, infrastructure, persistence, API, local runtime, testing |
| [[harness-architecture]] | Codex-facing harness definition, boundaries, layers, memory lanes, future runtime |
| [[harness-operational-policies]] | Hooks, policy, verification order, committed and ignored state, improvement loop |

## Agent Contract

Agents should enter through [[README|docs home]] or [[_memory/source-of-truth-map|source-of-truth map]], then read the smallest relevant source note here before editing code.

These files are authoritative memory, not derived summaries. MOCs and glossaries can index them, but must not replace them.
