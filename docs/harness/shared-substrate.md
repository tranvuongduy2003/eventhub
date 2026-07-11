---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.shared-substrate
title: Shared Substrate
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Shared Substrate

The reliable shared state for Codex work is the repository itself: files, tests, git status, and command output.

## Rules

- Treat prose plans as temporary; treat code, tests, contracts, and verification output as durable evidence.
- Use `git status --short` and diffs to understand the current workspace before making claims.
- Keep runtime notes under ignored `.codex/` paths when they are useful locally.
- Do not rely on stale progress notes when the working tree says something different.
- When using focused roles or subagents, give them raw artifacts and a clear task, not conclusions to rubber-stamp.

## Coordination

- One change should have one source of truth for acceptance criteria.
- Generated artifacts should be regenerated from their source input.
- Evidence should name commands and results, not merely say "verified".







