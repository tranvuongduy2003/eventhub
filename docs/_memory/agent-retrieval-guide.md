---
title: Agent Retrieval Guide
type: retrieval_guide
status: active
tags:
  - memory/retrieval
  - agents
---

# Agent Retrieval Guide

Use this guide when an agent needs durable EventHub context.

## Fast paths

- Need project rules: read [[CONSTITUTION]].
- Need product "why": read [[_memory/source/product-requirements]] and [[_memory/mocs/product-intent]].
- Need feature behavior: read [[_memory/source/feature-specification]] and the relevant file in `docs/_memory/specs/`.
- Need model rules: read [[_memory/source/domain-model-specification]] and [[_memory/mocs/domain-model]].
- Need implementation mechanics: read [[_memory/source/technical-design]] and [[_memory/mocs/technical-architecture]].
- Need Codex harness behavior: read [[_memory/source/harness-architecture]], [[_memory/source/harness-operational-policies]], and [[_memory/mocs/harness-memory]].

## Search keys

- Product decisions: `DEC-*`
- Quality guardrails: `QG-*`
- Epics and features: `EP-*`, `F-*`
- Domain model: `BC-*`, `AGG-*`, `VO-*`, `INV-*`, `EVT-*`
- Harness concepts: `hooks`, `policy`, `verify gate`, `evals`, `state`, `skills`

## Before code changes

1. Resolve the topic through a MOC.
2. Read the authoritative source doc linked by the MOC.
3. Read the relevant implementation spec if one exists.
4. Scout code paths after the document path is clear.
5. Verify with the smallest meaningful check.

## Memory update rule

When a task creates a durable decision, add it to the proper source document first. Add or adjust memory notes only to improve retrieval.
