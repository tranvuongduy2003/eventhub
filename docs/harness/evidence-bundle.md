---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.evidence-bundle
title: Evidence Bundle
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Evidence Bundle

For non-trivial changes, handoff should include enough evidence for another engineer to trust and reproduce the work.

## Contents

- Requirement summary.
- Changed files or areas.
- Verification commands run.
- Result for each command.
- Contract updates, if any.
- Tests added or changed.
- Known gaps and residual risk.

## Example

| Area | Command | Result |
| --- | --- | --- |
| Backend | `dotnet test EventHub.slnx -c Release` | pass |
| Frontend | `yarn --cwd web build` | pass |
| Contract | `yarn --cwd web api:verify` | pass |
| E2E | `yarn --cwd e2e test` | not run: local stack unavailable |

## Rules

- Do not claim a check passed unless it was run in this workspace.
- Prefer exact commands over vague labels.
- Include skipped checks when they are relevant to the risk of the change.







