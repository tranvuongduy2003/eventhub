---
title: Long-Term Memory Operating Model
type: memory_model
status: active
tags:
  - memory/model
  - harness/memory
---

# Long-Term Memory Operating Model

This note defines how EventHub docs act as durable long-term memory for product, domain, architecture, and harness knowledge.

## Principle

Long-term memory is not the active chat context and not temporary task state. It is durable, curated knowledge that should survive sessions, support retrieval, and reduce repeated rediscovery.

## Three memory lanes

| Lane | Location | Purpose | Commit? |
|---|---|---|---|
| Working memory | Active Codex conversation | Short-lived reasoning and task context | No |
| Task memory | `.codex/plans/`, `.codex/notes/`, eval outputs, handoff text | Operational artifacts for a specific task | Usually no |
| Long-term memory | `docs/` Obsidian vault | Product, domain, architecture, policies, decisions, durable maps | Yes |

## What belongs here

- Source-of-truth documents: [[CONSTITUTION]], [[_memory/source/product-requirements]], [[_memory/source/feature-specification]], [[_memory/source/domain-model-specification]], [[_memory/source/technical-design]].
- Durable harness policy and architecture: [[_memory/source/harness-architecture]], [[_memory/source/harness-operational-policies]].
- Implementation-ready product specs under `docs/_memory/specs/`.
- Index notes, maps of content, glossaries, and decision summaries that improve retrieval.

## What does not belong here

- Raw command output.
- Temporary progress logs.
- Large copied sections from source docs.
- Session-specific speculation that has not become a decision, spec, or accepted design.
- Secrets, environment data, local machine paths, or credentials.

## Promotion rules

Promote a fact into long-term memory when it is:

- a stable product or architecture decision;
- a cross-cutting invariant likely to be reused;
- a repeated retrieval path that saves agents from reading too much;
- a resolved open question from a spec;
- a harness policy or workflow rule.

Do not promote a fact when it is only a one-off implementation detail or an unresolved thought.


## Completion memory sync

When a `/cook` run completes a spec-backed feature or workflow change, update every durable memory surface that became stale. Do not stop at the first obvious index.

Use the `memory-sync` skill for this audit. Check this inventory and mark each item updated or explicitly `N/A` in the plan/handoff:

The inventory spans source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data, and owned handoff evidence.

- Related spec front matter and visible status header.
- Authoritative source docs when product, domain, technical, or harness contracts changed.
- MOCs: feature roadmap, product intent, domain model, technical architecture, and harness memory where relevant.
- Glossaries and decision logs when terms, decisions, invariants, or policies changed.
- Retrieval guides, source-of-truth maps, root `README.md`, `docs/README.md`, and `AGENTS.md` when navigation or workflow changed.
- Harness runtime contracts, routing, graph mappings, policy files, eval cases, scripts, and telemetry/tool registries when agent behavior changed.
- External tracking or handoff evidence, such as GitHub issue/PR status, when the current workflow owns it.

Run `scripts/agent/Test-DocsMemory.ps1` after docs memory changes and `scripts/agent/Verify-ChangedCode.ps1` before handoff.
## Retrieval contract

When using this vault:

1. Start from [[README]] or [[source-of-truth-map]].
2. Follow a MOC for the topic.
3. Read the authoritative source linked by the MOC before changing code.
4. If a memory note and source doc disagree, trust the source doc and update the memory note.
