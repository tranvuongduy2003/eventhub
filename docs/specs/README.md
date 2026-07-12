---
doc_schema: eventhub-doc-v1
doc_kind: index
doc_id: eventhub.specs.index
title: EventHub Implementation Specs Index
status: active
last_updated: 2026-07-12
owner: builder
language: en
applies_to: docs/specs/
---

# Implementation Specs Memory

This folder holds committed, implementation-ready product specs created or consumed by the `cook` workflow.
The current `cook` flow starts from a user idea, feature target, or existing spec; it uses strong
brainstorming and planning passes to make the spec detailed before implementation begins.

Canonical path: `docs/specs/`.

Specs are durable implementation evidence. They bridge the three source-of-truth specifications and engineering plans:

- `docs/product.md`, `docs/features.md`, and `docs/technical.md` define product, feature, domain, and technical contracts.
- Specs define a scoped feature slice with observable acceptance criteria, business rules, edge
  cases, verification strategy, and implementation notes detailed enough for faster implementation
  agents to execute without product guessing.
- Local `.codex/` scratch paths may hold ephemeral implementation plans derived from specs.

## Naming

Use `<YYYYMMDDHHmmss>-<feature-kebab>.md`.

## Completion Sync

When a spec-backed implementation completes, reconcile the relevant status and acceptance evidence in `docs/features.md`, update this spec when it remains current evidence, and refresh any affected `docs/harness/*`, `AGENTS.md`, skill, hook, or script guidance in the same change.

## Agent Contract

Agents should read the relevant spec here after `docs/product.md`, `docs/features.md`,
`docs/technical.md`, and the applicable `AGENTS.md` files, then use `cook` or the narrower owning
skill for implementation. If no suitable spec exists, `cook` should create or refine one before
coding.

Do not recreate the removed `_memory` vault or a parallel specs directory.
