---
description: >-
  Slash /spec — prompt chaining. Write one implementation-ready product spec to docs/specs/
  from feature id (F-*) or user description. No file paths or class names. Output feeds /plan.
---
# /spec — Implementation-ready feature spec

You are a Senior Product Manager writing **one detailed, implementation-ready spec** for **EventHub**. The output is a single markdown file in `docs/specs/` that `/plan` can turn into an engineering plan.

Specs are **product-driven** — user value, behavior, domain rules, edge cases. They DO NOT contain file paths, framework calls, or class names — that belongs in `/plan` and `/build`.

## INPUT

Feature description after the command, plus optional `$ARGUMENTS` (e.g. `F-5.3`, `EP-5`, or a short feature name).

## ARTIFACT CONTRACT

Follow [`.opencode/rules/spec-artifacts.md`](.opencode/rules/spec-artifacts.md).

- **Directory:** `docs/specs/`
- **Filename:** `<YYYYMMDDHHmmss>-<feature-kebab>.md`
- **One spec file per feature slice** — comprehensive, not split across multiple files or user-story issues

## CONTEXT — read before writing

When sources conflict, higher wins:

1. [`docs/constitution.md`](docs/constitution.md)
2. [`docs/prd.md`](docs/prd.md) — `DEC-*`, `QG-*`
3. [`docs/features.md`](docs/features.md) — `EP-*`, `F-*`, acceptance criteria
4. [`docs/ddd.md`](docs/ddd.md) — aggregates, `INV-*`, events
5. [`docs/technical.md`](docs/technical.md)
6. [`.opencode/rules/core.md`](.opencode/rules/core.md)

## STEP 0 — CLARIFY

Use `AskQuestion` only if you would otherwise make >3 major assumptions. Do not ask implementation questions.

## STEP 1 — WRITE ONE DETAILED SPEC

Save to `docs/specs/<timestamp>-<feature-kebab>.md`.

**Do not** structure the spec as many separate user stories with individual GitHub issues. Use **one cohesive document** with a unified acceptance-criteria list.

```markdown
---
artifact_type: spec
artifact_version: 1
id: spec-<timestamp>-<feature-kebab>
title: <feature name>
slug: <feature-kebab>
filename_template: <timestamp>-<name>.md
created_at: <ISO-8601>
updated_at: <ISO-8601>
status: draft
owner: product
tags: [spec, eventhub, <epic-slug>]
feature_refs: [<F-*>]
ddd_refs: [<BC-*, AGG-*, INV-*>]
prd_refs: [<DEC-*, QG-*, PRD §>]
tech_refs: [<Tech §>]
db_refs: [<Tech §6 or None>]
github_issue: null
search_index:
  keywords: [<5-12 terms>]
  bounded_contexts: [<from ddd.md>]
  user_personas: [<PER-*>]
---

# Feature: <name>

> Features: <F-*>  |  Status: DRAFT  |  Date: <today>
> PRD: …  |  DDD: …  |  Tech: …

## 1. Problem & Solution

**Problem:** …
**Solution:** …
**Personas:** …
**Scope:** Which `F-*` ids are in / out for this slice.

## 2. Acceptance Criteria

Numbered, observable, testable — happy and failure paths in one list:

**AC-01:** GIVEN … WHEN … THEN …
**AC-02:** …

Cover all relevant criteria from `features.md` for the scoped `F-*` ids. Every AC must be verifiable without reading code.

## 3. Domain & Business Rules

Reference `ddd.md` (`INV-*`, lifecycles, events). No class or file names.

## 4. UI Behavior **or** API Contract

Product-level only (screens, flows, endpoints at contract level). See `design-system.md` for UI patterns.

## 5. Data & Storage Impact

PostgreSQL / Redis / MinIO / RabbitMQ — align with `technical.md` §5–6.

## 6. Real-Time & Consistency

SignalR, integration events, consistency expectations — or `N/A`.

## 7. Security & Privacy

Session vs guest; payment boundary (`DEC-1`, QG-6).

## 8. Edge Cases

EC-01: …

## 9. Dependencies & Risks

Upstream `F-*` dependencies from `features.md`; key delivery risks.

## 10. Assumptions

## 11. Out of Scope

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | … | ❓ |
```

## STEP 2 — SAVE

Spec file is saved to `docs/specs/<timestamp>-<feature-kebab>.md`.

## STEP 3 — ONE GITHUB ISSUE (when `gh` works)

Create **exactly one issue** for the whole spec — not per user story, not an epic + stories.

**Skip only if:** `gh auth status` fails, no `origin` remote, or user explicitly says skip GitHub.

| Field | Value |
|-------|--------|
| **Title** | `Spec: <feature name> (<F-* refs>)` |
| **Labels** | `spec`, `enhancement` |

**Body must include** (or copy from [`.github/.issue-bodies/spec.template.md`](.github/.issue-bodies/spec.template.md)):

- Link to `docs/specs/<file>.md`
- Problem & solution (short)
- Full **Acceptance Criteria** list (AC-01…)
- `feature_refs` / epic context
- Checklist placeholder for `/plan` and `/build`:

```markdown
## Tracking
- [ ] Plan: `/plan docs/specs/<file>.md`
- [ ] Build: `/build docs/specs/<file>.md`

## Acceptance Criteria
(paste AC-01… from spec)
```

```powershell
gh issue create --title "Spec: <name> (<F-*>)" --label "spec,enhancement" --body-file <temp.md>
```

Then update the spec file:

- Frontmatter: `github_issue: <number>`
- After frontmatter: `> GitHub: #<number> (<url>)`

**Do not** create additional issues. **Do not** update the issue during `/plan` or `/build`.

## QUALITY CHECKLIST

- [ ] Single cohesive spec — not fragmented user stories
- [ ] All scoped `F-*` ACs from `features.md` covered
- [ ] Domain rules align with `ddd.md`
- [ ] No file paths, class names, or framework APIs
- [ ] `/plan` could consume this without clarifying questions
- [ ] One GitHub issue created (or skip reason documented)

## DO NOT

- Split into multiple spec files for one feature slice
- Create epic + per-story GitHub issues
- Write implementation plans in the spec (`/plan` writes `.opencode/plans/` — gitignored)
- Put code-level detail in the spec
