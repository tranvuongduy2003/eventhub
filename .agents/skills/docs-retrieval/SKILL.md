---
name: docs-retrieval
description: Retrieve precise EventHub context from the Obsidian-backed docs vault. Use when answering questions from docs, planning work from product/domain/technical memory, resolving source-of-truth conflicts, looking up stable IDs such as DEC/QG/EP/F/BC/AGG/INV/EVT, or deciding which docs in docs/ must be read before code edits.
---

# Docs Retrieval

Use this skill to find the smallest authoritative EventHub documentation context before answering, planning, or editing. The docs vault is optimized for Obsidian links, MOCs, source memory, and stable IDs; do not read the whole vault unless the task truly spans the whole product.

For conflict-heavy or accuracy-critical retrieval, also read `rules/obsidian-docs.md`.

## Retrieval Order

1. Start with `docs/CONSTITUTION.md` when architecture, data, API, local topology, naming, testing, or rule precedence matters.
2. Use `docs/_memory/source-of-truth-map.md` to choose the authoritative source lane.
3. Use one MOC only when the task is broad or ambiguous:
   - Product: `docs/_memory/mocs/product-intent.md`
   - Features/specs: `docs/_memory/mocs/feature-roadmap.md`
   - Domain model: `docs/_memory/mocs/domain-model.md`
   - Technical architecture: `docs/_memory/mocs/technical-architecture.md`
   - Harness: `docs/_memory/mocs/harness-memory.md`
4. Read the owning source document under `docs/_memory/source/`.
5. Read the relevant spec in `docs/_memory/specs/` when a feature implementation, acceptance criteria, or handoff status is needed.
6. Search code only after the document lane is clear.

## Source Lanes

| Need | Read |
|---|---|
| Product intent, personas, scope, guardrails, decisions | `docs/_memory/source/product-requirements.md` |
| Epics, feature IDs, acceptance criteria, build order | `docs/_memory/source/feature-specification.md` |
| Bounded contexts, aggregates, invariants, events | `docs/_memory/source/domain-model-specification.md` |
| Clean Architecture, CQRS, persistence, API, tests | `docs/_memory/source/technical-design.md` |
| Harness boundaries, layers, runtime contract | `docs/_memory/source/harness-architecture.md` |
| Hooks, policies, state, verification, eval loop | `docs/_memory/source/harness-operational-policies.md` |
| Durable feature specs and implementation status | `docs/_memory/specs/` |
| Cross-vault navigation | `docs/_memory/agent-retrieval-guide.md`, `docs/_memory/source-of-truth-map.md`, `docs/README.md` |

## Search Rules

Use `rg` first. Prefer stable IDs and exact domain terms over broad words.

```powershell
rg -n "DEC-[0-9]+|QG-[0-9]+|EP-[0-9]+|F-[0-9.]+|BC-[0-9]+|AGG-[0-9]+|INV-[0-9]+|EVT-[0-9]+" docs
rg -n "<feature-slug>|<domain-term>|<route>|<aggregate-name>" docs/_memory/source docs/_memory/specs
rg -n "\[\[.*<term>.*\]\]|<term>" docs/_memory/mocs docs/_memory/glossary docs/_memory/source
```

When a query mentions a feature name, search `docs/_memory/specs/` by slug and then confirm against `feature-specification.md`. When it mentions a domain rule, search `domain-model-specification.md` first, then specs.

## Accuracy Rules

- Treat precedence as Constitution -> source memory -> harness source memory -> MOCs/glossaries/retrieval guides -> scoped rules/skills/plans.
- Cite or summarize from source documents, not only from MOCs, unless the user only needs navigation.
- If two docs conflict, trust the higher-precedence source and flag lower-level drift.
- Do not invent implementation status. Confirm from the relevant spec front matter, visible status text, or current code/tests.
- Do not use `.codex/plans/`, `.codex/notes/`, or `.codex/state/` as durable truth; they are task memory.
- Do not edit generated output, `web/src/generated/`, or OpenAPI generated artifacts while retrieving docs context.

## Minimal Context Budget

Read the smallest set that can answer the question:

- Simple rule lookup: Constitution or one source doc.
- Feature behavior: `feature-specification.md` plus the matching spec.
- Domain behavior: `domain-model-specification.md` plus the matching feature/spec if behavior is user-facing.
- API/persistence mechanics: Constitution plus `technical-design.md`, then relevant spec/domain doc.
- Harness workflow: harness source doc(s), then the owning harness skill only if procedure is needed.

Stop reading once the answer is supported by authoritative text and one targeted search shows no contradictory stable ID or spec.

## Updating Memory

Use `memory-sync` when work changes durable knowledge or completes a spec-backed workflow. Update source docs first, then affected MOCs, glossaries, retrieval guides, indexes, README files, harness contracts, graph/routing data, and handoff evidence.

Run docs validation after memory edits:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1
```

Run the standard changed-code verification before handoff:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```
