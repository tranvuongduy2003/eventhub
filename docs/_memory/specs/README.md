---
title: Implementation Specs Memory
type: spec_index
status: active
tags:
  - memory/specs
  - product/specs
---

# Implementation Specs Memory

This folder holds committed, implementation-ready product specs created by the `spec` skill.

Canonical path: `docs/_memory/specs/`.

Specs are durable long-term knowledge memory. They bridge source memory and engineering plans:

- Source memory defines product, domain, and technical contracts.
- Specs define a scoped feature slice with observable acceptance criteria.
- `.codex/plans/` holds ephemeral implementation plans derived from specs.

## Naming

Use `<YYYYMMDDHHmmss>-<feature-kebab>.md`.

## Agent Contract

Agents should read the relevant spec here after the constitution and source memory, then create or consume paired plans under `.codex/plans/`.

Do not recreate the legacy loose specs directory outside `_memory`.
