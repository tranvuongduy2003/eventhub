---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.permission-tiers
title: Permission Tiers
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Permission Tiers

Use the lowest permission tier that can complete the work.

## Tier 0: Read and Orient

Allowed:

- Read files.
- Search with `rg`.
- Inspect git status and diffs.
- Run harmless metadata commands.

Use for requirements analysis, code review, and planning.

## Tier 1: Local Edits

Allowed:

- Edit source, tests, docs, and committed config.
- Run formatters, builds, unit tests, integration tests, and E2E tests.
- Generate OpenAPI and TypeScript schema artifacts through repository scripts.

Use for ordinary implementation.

## Tier 2: Sensitive Local Operations

Requires explicit care:

- EF Core migration generation.
- Recursive filesystem operations.
- Commands that create or remove local infrastructure state.
- Changes to hook scripts, guardrails, authentication, authorization, sessions, uploads, or secrets handling.

Verify paths before recursive operations and run focused regression checks.

## Tier 3: External or Irreversible Operations

Requires explicit user instruction:

- Force push or history rewrite.
- Publishing packages or deployments.
- Accessing secrets or private credentials.
- Production-impacting commands.
- Destructive operations outside the repository.

The guardrail hook blocks known unsafe command patterns. Human confirmation is still required for operations that are risky but not mechanically detectable.







