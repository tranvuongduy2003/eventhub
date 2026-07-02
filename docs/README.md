---
title: EventHub Knowledge Memory
type: vault_home
status: active
tags:
  - memory/index
  - eventhub
---

# EventHub Knowledge Memory

This `docs/` folder is an Obsidian vault and the long-term knowledge memory for EventHub.

The source memory documents in [[_memory/source/README|Authoritative Source Memory]] remain authoritative. MOCs, glossaries, and retrieval guides add paths and cross-links so humans and agents can find the right context without reading the entire repository.

## Start here

- [[CONSTITUTION|Constitution]] - immutable architecture, data, API, local run, naming, and testing rules.
- [[_memory/source/product-requirements|Product Intent]] - why EventHub exists, personas, goals, guardrails, and `DEC-*` decisions.
- [[_memory/source/feature-specification|Feature Specification]] - epics, feature IDs, phases, dependencies, and acceptance criteria.
- [[_memory/source/domain-model-specification|Domain Model]] - bounded contexts, aggregates, invariants, domain events, and integration events.
- [[_memory/source/technical-design|Technical Design]] - Clean Architecture, CQRS, infrastructure, persistence, API, and testing mechanics.
- [[_memory/source/harness-architecture|Harness Architecture]] - Codex-facing harness contract.
- [[_memory/source/harness-operational-policies|Harness Operational Policies]] - hooks, policy, verification, state, and improvement loop.

## Memory maps

- [[_memory/source-of-truth-map|Source Of Truth Map]]
- [[_memory/long-term-memory-operating-model|Long-Term Memory Operating Model]]
- [[_memory/agent-retrieval-guide|Agent Retrieval Guide]]
- [[_memory/mocs/product-intent|Product Intent MOC]]
- [[_memory/mocs/feature-roadmap|Feature Roadmap MOC]]
- [[_memory/mocs/domain-model|Domain Model MOC]]
- [[_memory/mocs/technical-architecture|Technical Architecture MOC]]
- [[_memory/mocs/harness-memory|Harness Memory MOC]]

## Glossary and durable facts

- [[_memory/glossary/ubiquitous-language|Ubiquitous Language]]
- [[_memory/glossary/decision-log|Decision Log]]
- [[_memory/glossary/architecture-invariants|Architecture Invariants]]

## Memory lanes

- Working memory: current Codex conversation context. It is temporary.
- Task memory: local plans, notes, eval results, and handoffs. It lives in ignored `.codex/` state when it should not be committed.
- Long-term knowledge memory: committed, curated Markdown in this vault. It contains source docs, MOCs, glossaries, and templates.

## Rules

- Do not duplicate source-of-truth content unless the duplicate is a short index, glossary entry, or retrieval aid.
- When a memory note conflicts with a source document, fix the memory note. The precedence is [[CONSTITUTION]] first, then product/domain/technical source memory, then harness source memory, then memory notes.
- Use stable IDs in titles and links where they exist: `DEC-*`, `QG-*`, `EP-*`, `F-*`, `BC-*`, `AGG-*`, `INV-*`, `EVT-*`.
- New implementation specs belong in `docs/_memory/specs/`. Memory summaries can point to them, but should not replace them.
