---
status: active
workflow: cook
---

# Incomplete Cook Plan

## Harness Impact

| Lane | Impact | Files | Validation |
|------|--------|-------|------------|
| evals | N/A | N/A | N/A |
| orchestrator | N/A | N/A | N/A |
| policies | N/A | N/A | N/A |
| telemetry | N/A | N/A | N/A |
| tools | N/A | N/A | N/A |
| workflow | N/A | N/A | N/A |

## Memory Sync Inventory

| Surface | Status | Notes |
|---------|--------|-------|
| Related spec | N/A | No spec. |
| Source docs | N/A | No source changes. |
| MOCs/glossaries/retrieval guides | N/A | No memory changes. |
| README/index files | N/A | No route changes. |
| Harness contracts and graph/routing | N/A | No harness changes. |
| External tracking | N/A | No external tracking. |

## Tasks

- [ ] Implement backend-only work.
- [ ] Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1`.

## Done Criteria Ledger

- [ ] Acceptance criteria verified.
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1` passed.
