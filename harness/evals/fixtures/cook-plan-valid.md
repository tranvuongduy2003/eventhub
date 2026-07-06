---
status: active
workflow: cook
---

# Workflow Repair Plan

## Adjacent Feature Boundary

In scope: repair cook workflow validation.

Out of scope: product feature implementation.

Neighboring features: N/A.

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
| Related spec | planned | Mark implemented after checks pass. |
| Source docs | N/A | Source feature boundary already current. |
| MOCs/glossaries/retrieval guides | planned | Update feature roadmap. |
| README/index files | N/A | No route changes. |
| Harness contracts and graph/routing | N/A | Product slice only. |
| External tracking | N/A | No external tracking. |

## Surface Completeness Review

| Surface | Impact | Action | Validation |
|---------|--------|--------|------------|
| Backend/API | N/A | No product backend change. | N/A |
| Frontend/web | N/A | No user-facing route or component change. | N/A |
| OpenAPI/codegen | N/A | No endpoint or contract shape change. | N/A |
| E2E/Playwright | N/A | No browser workflow change. | N/A |
| DevOps/Aspire | N/A | No topology, config, or runtime dependency change. | N/A |
| Docs/memory | planned | Update harness memory for workflow contract changes. | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1` |
| Harness/workflow | planned | Update cook validator and eval fixture. | `powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness` |

## Tasks

- [ ] Add cook workflow validator.
- [ ] Add harness eval coverage.
- [ ] Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1`.

## Done Criteria Ledger

- [ ] Acceptance criteria verified.
- [ ] Surface completeness review completed.
- [ ] Memory sync completed.
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1` passed.
