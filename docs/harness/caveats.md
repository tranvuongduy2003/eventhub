---
doc_schema: eventhub-doc-v1
doc_kind: harness_doc
doc_id: harness.caveats
title: Caveats
status: active
last_updated: 2026-07-11
owner: codex
language: en
applies_to: codex_harness
---

# Caveats

The harness reduces risk, but it does not replace engineering judgment.

## Green Checks Are Not Complete Proof

A build or test suite can pass while the delivered behavior still misses the user's request. Always compare the final behavior against acceptance criteria.

## Hooks Are Guardrails, Not Policy Exhaustiveness

Hooks catch known unsafe patterns. They cannot recognize every risky operation or every contextual exception. Use explicit user confirmation for external, destructive, or irreversible work.

## Generated Artifacts Need Source Discipline

Generated files are useful evidence only when their source inputs are updated and the generation command is known.

## Verification Has Cost

Run the narrowest useful checks while iterating. Broaden verification before handoff when the change touches shared behavior, authentication/session handling, persistence, contracts, or cross-layer flows.

## Review the Harness Periodically

Re-run guardrail sensors after changing hooks or repository structure:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1
```






