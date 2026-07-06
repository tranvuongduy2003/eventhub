---
name: cook
description: End-to-end EventHub delivery workflow. Use for product specs, engineering plans, implementation, verification, and repair loops through one harness-owned entrypoint.
---

# Cook

`cook` is the single EventHub feature-delivery skill with one phased workflow:

`intake -> spec -> plan -> checkpoint loop -> verify -> memory sync -> handoff`

The harness owns orchestration, state, validation, approvals, and stop conditions. The working tree, shell, and subagents are execution surfaces only.

> PLAN SYNC: Deviations update the current artifact immediately. Mark tasks done only after objective checks pass.

> EPHEMERAL PLAN: `.codex/plans/` only. Never commit. Delete only when the run completes and all required memory sync is done.

## Step 0: Read context

Read the smallest relevant set:

`docs/CONSTITUTION.md` · `docs/_memory/source/product-requirements.md` · `docs/_memory/source/feature-specification.md` · `docs/_memory/source/domain-model-specification.md` · `docs/_memory/source/technical-design.md` · `docs/_memory/source/harness-architecture.md` · `docs/_memory/source/harness-operational-policies.md` · existing related spec or plan · current task notes.

When sources conflict: Constitution -> source memory -> harness source docs -> scoped rule -> this skill.

For docs-heavy PowerShell reads, use `Get-Content -Encoding UTF8` so punctuation in source memory and skill text remains readable.

## Step 1: Parse input

| Input | Action |
|-------|--------|
| Feature/user request | Run from intake through done unless the user asks for a stop phase |
| Feature/user request with `--dry-run`, `dry-run`, `audit`, or `trace-only` | Run intake/context planning as a no-write audit. Produce only a concise trace/report in chat or an explicit user-approved inbox note. Do not create product specs, plans, progress notes, code changes, or memory updates. The trace must list intended durable artifacts, adjacent-feature boundaries, likely validation, and any ambiguity that would block a real run. |
| `.codex/plans/<file>.md` | Resume from the plan phase and implement |
| `docs/_memory/specs/<file>.md` | Use that spec and create or resume the paired plan |
| `task N` | Resume at task N |
| Newest `.codex/plans/` | Use only when no path or feature request is provided |

Use the full workflow by default. Stop after an intermediate artifact only when the user explicitly asks for a spec-only or plan-only outcome.

Branch: `feature/<slug>` from the spec or plan. Do not create or switch branches unless the user asked for branch work.

## Step 2: Artifact Contract

Produce artifacts in the existing repository locations:

| Artifact | Location | Commit? |
|----------|----------|---------|
| Spec Markdown | `docs/_memory/specs/<timestamp>-<slug>.md` | yes |
| Plan Markdown | `.codex/plans/<same-filename>.md` | no |
| Progress notes | `.codex/notes/progress.md` | no |
| Harness runtime state | `.codex/state/` | no |
| Eval evidence | `harness/evals/results/` | no |

Specs and plans must be structured enough for the harness to parse their status, Harness Impact, Memory Sync inventory, tasks, and validation commands. The runtime schema for machine-readable task shape lives in `harness/orchestrator/task-spec.schema.json`.

## Step 3: Spec Phase

Write one implementation-ready product spec in `docs/_memory/specs/` when the request does not already provide one.

The spec is product-driven: user value, observable behavior, domain rules, edge cases, and acceptance criteria. Do not put code-level file paths, class names, or framework details in the spec.

Required sections:

- Problem and solution
- Acceptance criteria
- Domain and business rules
- UI behavior or API contract
- Data, real-time, security, edge cases, dependencies, assumptions, out of scope
- `## Adjacent Feature Boundary` for every feature-id run. Name dependent or neighboring features, state what is in this slice, and state what remains out of scope.
- `## 7. Harness Impact`

`## 7. Harness Impact` is mandatory. It must state impacts for `harness/evals/`, `harness/orchestrator/`, `.codex/policies/` or `harness/policies/`, `harness/telemetry/`, `harness/tools/`, and workflow surfaces such as `harness/graph/`, `.agents/skills/`, `.codex/hooks/`, `scripts/agent/`, or `AGENTS.md`. Use `N/A - product slice only; no harness behavior changes.` only when all are unaffected.

If the user asked to stop after spec, run docs-memory checks for changed durable memory, report the spec path, and stop.

Immediately after creating a durable spec, update the relevant long-term memory discovery surface before planning starts. Feature specs normally update `docs/_memory/mocs/feature-roadmap.md`; other durable specs may update source maps, MOCs, glossaries, retrieval guides, or README/index files. Run `scripts/agent/Test-DocsMemory.ps1` when durable docs memory changes.

## Step 4: Plan Phase

Create or update `.codex/plans/<same-filename-as-spec>.md`.

Every plan must include these sections before tasks:

```markdown
## Harness Impact

| Lane | Impact | Files | Validation |
|------|--------|-------|------------|
| evals | N/A or ... | `harness/evals/...` | `powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness` |
| orchestrator | N/A or ... | `harness/orchestrator/...` | ... |
| policies | N/A or ... | `.codex/policies/...` / `harness/policies/...` | ... |
| telemetry | N/A or ... | `harness/telemetry/...` | ... |
| tools | N/A or ... | `harness/tools/...` | ... |
| workflow | N/A or ... | `harness/graph/...` / `.agents/skills/...` / `.codex/hooks/...` / `scripts/agent/...` / `AGENTS.md` | ... |

## Memory Sync Inventory

| Surface | Status | Notes |
|---------|--------|-------|
| Related spec | planned/N/A | ... |
| Source docs | planned/N/A | ... |
| MOCs/glossaries/retrieval guides | planned/N/A | ... |
| README/index files | planned/N/A | ... |
| Harness contracts and graph/routing | planned/N/A | ... |
| External tracking | planned/N/A | ... |

## Surface Completeness Review

| Surface | Impact | Action | Validation |
|---------|--------|--------|------------|
| Backend/API | planned/N/A | ... | ... |
| Frontend/web | planned/N/A | ... | ... |
| OpenAPI/codegen | planned/N/A | ... | ... |
| E2E/Playwright | planned/N/A | ... | ... |
| DevOps/Aspire | planned/N/A | ... | ... |
| Docs/memory | planned/N/A | ... | ... |
| Harness/workflow | planned/N/A | ... | ... |
```

Then write 3-8 concrete tasks. Map every acceptance criterion to at least one task and map every impacted surface from the Surface Completeness Review to at least one task or done-criteria item. Include paths, notes, and validation commands. Harness changes must be visible as their own task or subtask, not hidden inside product work.

Surface completeness is mandatory for feature work. Do not mark `Frontend/web`, `E2E/Playwright`, `DevOps/Aspire`, or `OpenAPI/codegen` as `N/A` just because the first implementation path is backend-heavy. Mark a surface `N/A` only with a product/technical rationale, for example "no user-visible route or component changes", "no browser workflow crosses this behavior", "no endpoint or contract shape changed", or "no topology/config/runtime behavior changed". If the feature changes an attendee or organizer workflow, include frontend work or explicitly record why the existing UI already satisfies the acceptance criteria. If the feature changes a critical user journey, add or update Playwright coverage unless a narrower automated check is objectively sufficient and the plan records that rationale. If the feature changes API or Contracts, include OpenAPI export/codegen/verify. If the feature changes local topology, configuration, background services, CI, deployment scripts, or runtime dependencies, include DevOps/Aspire validation.

Every plan must also include:

- `## Adjacent Feature Boundary` for feature-id runs, matching the spec boundary.
- `## Done Criteria Ledger` with checkboxes for acceptance criteria, surface completeness review, memory sync, harness validation when changed, docs-memory validation when changed, changed-code verification, and review/rationale.
- A reference to `.codex/notes/progress.md` for long runs.

Before implementation starts, create or update a TaskSpec sidecar under `.codex/state/cook/<task-id>.json` using `harness/orchestrator/task-spec.schema.json`, then validate the markdown plan and progress note:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-CookPlan.ps1 -PlanPath .codex/plans/<same-filename-as-spec>.md -ProgressPath .codex/notes/progress.md
```

If the user asked to stop after plan, report the plan path and stop.

## Step 5: Context Retrieval

Before each implementation task, use parallel read-only scouts when useful:

| Agent | When |
|-------|------|
| `@agent-graph-impact-analyst` | Blast radius and callers |
| `@agent-codebase-explorer` | Locate files and symbols |
| `@agent-test-impact-analyzer` | Determine focused checks for the current diff |
| `@agent-plan-domain-researcher` | Domain-heavy task |
| `@agent-plan-application-researcher` | CQRS/Application task |
| `@agent-plan-infrastructure-researcher` | Persistence, API, contract, or integration task |
| `@agent-plan-web-researcher` | Frontend task |

Subagents are read-only except explicit test-writer agents for red tests. The parent agent owns production code edits.

If subagent tools are unavailable, fall back to `rg`, `rg --files`, and targeted source reads. Use `rg --files` before reading guessed paths, record the scout fallback and key evidence in the plan or `.codex/notes/progress.md`, and continue with parent-agent edits.

## Step 6: Checkpoint Loop

For each unchecked task:

1. For bug fixes, add a focused red test first when feasible.
2. Implement the smallest change in layer order: Domain -> Application -> Infrastructure -> Api -> web -> harness workflow.
3. Run affected checks.
4. Fix only verified failures.
5. Mark the task complete only when checks pass.
6. Update the plan and `.codex/notes/progress.md` when new harness impact, memory drift, or blockers appear.
7. Keep the Done Criteria Ledger current; do not leave final criteria to chat memory.

Use `node scripts/affected-tests.mjs <path>` for changed files and run the returned checks.

Run these additional checks when relevant:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer harness
powershell -NoProfile -ExecutionPolicy Bypass -File harness/evals/run.ps1 -Layer graph
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1
```

## Step 7: Stop Conditions

Complete only when all apply:

- Required acceptance criteria are satisfied.
- Changed-code verification passes.
- Harness lane validation passes when any harness lane changed.
- Docs-memory validation passes after durable memory changes.
- Related spec status and affected long-term memory surfaces are synchronized.
- No high-severity review finding, policy denial, or unresolved approval remains.
- The active plan's Done Criteria Ledger is complete or explicitly marked `N/A` with rationale.

Blocked states must record the policy denial, missing external state, repeated verifier failure, or required human decision.

## Step 8: Memory Sync And Handoff

Use `memory-sync` before final handoff for spec-backed or workflow changes.

For completed spec-backed work, mark the related spec `implemented` only after objective checks pass and Memory Sync is complete. Update source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data, and handoff evidence when they describe the changed behavior.

Final verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

For substantial work, run or record why you skipped an evidence-based code review.

## Do Not

- Create a parallel `harness/cook/` architecture outside the existing harness contracts.
- Add a root `evals/` tree.
- Hide harness changes inside product tasks.
- Commit `.codex/plans/**`, `.codex/notes/**`, `.codex/state/**`, or `harness/evals/results/**`.
- Edit protected generated files such as `web/src/generated/` or `contracts/openapi/.build/`.
