---
name: memory-sync
description: Audit and update EventHub long-term memory after spec-backed implementation or workflow changes. Use when a feature/spec is completed, when /cook reaches handoff, when docs memory may be stale, or when changes touch source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data, GitHub issue/PR status, or handoff evidence.
---

# Memory Sync

Keep EventHub durable memory aligned with completed work. Use this skill after implementing a spec-backed feature or workflow change, before final handoff or PR creation.

## Read First

Read the smallest relevant set:

1. `docs/_memory/long-term-memory-operating-model.md`
2. `docs/_memory/source-of-truth-map.md`
3. `docs/_memory/source/harness-operational-policies.md`
4. Related spec in `docs/_memory/specs/`
5. Any source doc or MOC named by the spec, plan, or changed files

If a source doc and derived memory conflict, update derived memory unless the task explicitly changes the source of truth.

## Inventory

Create a Memory Sync inventory in the plan or handoff. Mark each row `updated` or `N/A` with a short reason.

| Surface | Check |
|---|---|
| Related spec | Front matter `status`, `updated_at`, visible status header, resolved open questions |
| Feature roadmap | Owning `EP-*` section links the spec; catch-all text remains true |
| Source docs | Product, feature, domain, technical, and harness source docs reflect changed contracts |
| MOCs | Product intent, feature roadmap, domain model, technical architecture, and harness memory are current |
| Glossaries | Ubiquitous language, decision log, and architecture invariants include new stable terms/decisions/invariants |
| Retrieval/index docs | Source-of-truth map, agent retrieval guide, docs README, specs README, root README, and AGENTS.md route future agents correctly |
| Harness contracts | `harness/orchestrator/`, `harness/policies/`, `harness/telemetry/`, `harness/tools/`, `.codex/policies/`, hooks, scripts, skills, `.graph/` |
| Verification graph | `.graph/index.json` and `scripts/affected-tests.mjs` route changed memory and harness files to checks |
| External tracking | GitHub issue/PR/project status, labels, or handoff evidence when the current workflow owns them |

Do not stop at the first obvious MOC. The point is to make the next agent find the truth from any normal entry point.

## Workflow

1. Read the related spec and current diff.
2. Identify feature IDs, epics, bounded contexts, PRD decisions, DDD refs, tech refs, and harness lanes affected.
3. Search for stale references using stable IDs and feature terms:
   - `rg -n "F-x.y|EP-x|<feature slug>|<old status>|<key domain term>" docs AGENTS.md README.md harness .graph scripts .agents`
4. Update only durable surfaces that became stale.
5. For completed features, mark the related spec implemented only after objective implementation checks pass.
6. Keep source-of-truth precedence: Constitution -> source memory -> harness source memory -> MOCs/glossaries/retrieval guides -> task artifacts.
7. Run validation.

## Status Rules

- `draft`: spec exists but implementation is not verified complete.
- `implemented`: objective checks passed and Memory Sync is complete.
- Do not mark source `feature-specification.md` features as implemented unless that source document explicitly gains implementation-status tracking. Use spec status and MOCs for implementation state.

## Validation

Always run after docs memory changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1
```

Run before handoff:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

If harness workflow, policy, routing, skill, hook, graph, script, or runtime contract changed, also run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Get-HarnessStatus.ps1 -Json
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness
```

If `.graph/index.json` or `scripts/affected-tests.mjs` changed, also run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer graph
```

## Handoff Format

Report:

- Memory surfaces updated
- Surfaces checked and left `N/A`
- Validation commands and pass/fail result
- Residual drift or blocked surfaces

## Do Not

- Do not edit generated OpenAPI build output or `web/src/generated/`.
- Do not weaken source-of-truth docs to match stale MOCs.
- Do not create root `evals/` or ad-hoc docs folders.
- Do not mark a spec implemented before objective checks and Memory Sync pass.
- Do not update GitHub state unless the active workflow and available GitHub MCP tools explicitly own it.
