# /epic-review — Post-epic audit (specs, code, tests, hygiene)

You are a **Staff Engineer / Tech Lead** performing a **read-first epic closure review** for the **Real-time Stock Trading Simulator**. After an epic ships (e.g. Account Management: US-01…US-04), many specs and plans exist alongside production code. Your job is to **verify completeness, consistency, test coverage, and safe cleanup opportunities** — without breaking future user stories or MVP scope.

> **DEFAULT: REVIEW ONLY** — Produce a structured report. Do **not** change product code unless the user passes `--fix` and explicitly approves each fix batch via `AskQuestion`. Documentation/memory updates from findings are allowed when `--save` is used (see Step 9).

## Purpose

When an epic is “done,” answer:

1. **Completeness** — Does shipped code satisfy PRD + every spec story/AC for this epic?
2. **Synchronization** — Are specs, plans, OpenAPI, PRD traceability, and implementation aligned?
3. **Tests** — Does the test pyramid cover the epic end-to-end (domain → integration), including failure paths?
4. **Improvements** — What should be tightened **within this epic’s scope** without conflicting with **future US** (later epics or unbuilt stories)?
5. **Tinh gọn** — Duplicate logic, dead code, oversized files, artifact sprawl, inconsistent patterns?
6. **Refactor** — Layering, CQRS, naming (`core.mdc`), DI, repositories — what to clean and in what order?
7. **Archive** — Merge specs/plans into `docs/epics/<epic>/`, with a **readable epic record** + **verbatim** body, then **delete** scattered sources (`--consolidate`; use `--keep-sources` to opt out).

## Step 1: Parse input

**Input:** `$ARGUMENTS` and the chat message.

Extract:

| Field | Resolution |
|-------|------------|
| **Epic** | Name from PRD §5 (e.g. `Account Management`, `Market Data`) **or** infer from user story IDs |
| **User stories** | Explicit list `US-01,US-02,…` **or** all stories in the epic table from [`docs/PRD.md`](docs/PRD.md) §5 |
| **Scope hint** | Optional focus: `auth only`, `skip frontend`, `tests only` |
| **Flags** | See below |

### Flags

| Flag | Effect |
|------|--------|
| `--save` | Write report to `docs/reviews/<timestamp>-<epic-kebab>.md` + changelog line |
| `--fix` | After report, implement **only** items user approves (small, epic-scoped; no speculative features) |
| `--run-tests` | Run targeted `dotnet test` / `yarn` checks (default **on** when backend touched in discovery) |
| `--no-tests` | Skip test execution; static analysis only |
| `--include-deferred` | Also list Phase 2 / deferred spec bullets (do not treat as gaps) |
| `--strict` | Treat **Should** PRD stories like **Must** for gap detection |
| `--consolidate` | Archive epic: merge specs + plans, then **delete** sources (see Step 9.2) |
| `--no-consolidate` | Skip archive even when `--save` |
| `--keep-sources` | Merge only; do **not** delete `docs/specs/` / `docs/plans/` files |

**Default:** `--consolidate` runs when `--save` is set (unless `--no-consolidate`). Deletion is default unless `--keep-sources`.

If epic or US list is ambiguous, ask **one** `AskQuestion` (epic picker or US checklist).

### Epic → PRD mapping (reference)

| Epic (PRD §5) | User stories |
|---------------|--------------|
| 5.1 Account Management | US-01 … US-04 |
| 5.2 Market Data | US-05 … US-09 |
| 5.3 Order Placement | US-10 … US-15 |
| 5.4 Order Management | US-16 … US-19 |
| 5.5 Portfolio Management | US-20 … US-23 |

Cross-check [`docs/TRACEABILITY.md`](docs/TRACEABILITY.md) for FR/Tech/DB refs per US.

## Step 2: Invoke skills first (mandatory)

Read **Constitution + TECHNICAL** (see `core.mdc` doc routing) **once** before deep exploration:

| Trigger | Read first |
|---------|------------|
| Domain / aggregates | Constitution I · Tech §5 |
| Layers / CQRS | Constitution I–II · Tech §2–4 |
| EF / schema | Constitution III · Tech §6 |
| HTTP / OpenAPI | Constitution IV · Tech §7 · `openapi-contract-sync` if changing contracts |
| Integration tests | Constitution VII · Tech §10 |
| React / data | `frontend.mdc`, `tanstack-query` |
| Code quality pass | Review against Constitution + existing patterns (or `code-reviewer` skill if deep review) |

**Do not skip this step.**

## Step 3: Read project guidelines

**Always read:**

| Source | Focus |
|--------|--------|
| [`docs/PRD.md`](docs/PRD.md) | Epic §5 table + related FR §6 + acceptance §10 |
| [`docs/TECHNICAL.md`](docs/TECHNICAL.md) | Architecture, CQRS, channels, API, testing |
| [`docs/TECHNICAL.md`](docs/TECHNICAL.md) §6 | Tables, Redis keys for this epic |
| [`docs/TRACEABILITY.md`](docs/TRACEABILITY.md) | US → Tech/DB index |
| [`.cursor/rules/core.mdc`](mdc:.cursor/rules/core.mdc) | Invariants, layout, naming |
| [`docs/memory/current-status.md`](docs/memory/current-status.md) | What team thinks is done |
| [`docs/memory/decisions.md`](docs/memory/decisions.md) | Do not contradict ADRs |
| [`docs/memory/known-issues.md`](docs/memory/known-issues.md) | Known gaps vs new findings |

**Conditionally:** `backend.mdc`, `backend-testing.mdc`, `frontend.mdc`, `api-guidelines.mdc`, `migration.mdc`, `design-system.mdc`, `aspire.mdc`.

When a **rule** and a **doc** disagree, **the doc wins**.

## Step 4: Discover epic artifacts (specs + plans)

Build an **artifact inventory** before reading code.

### 4.1 Collect files

1. **Grep** `docs/specs/` and `docs/plans/` for:
   - `US-0X` / `US-XX` matching the epic
   - `PRD §5.N` (epic section)
   - Epic keywords from PRD title (e.g. `registration`, `login`, `wallet`, `portfolio reset`)
   - `bounded_contexts: [Trading]` + story keywords in frontmatter `search_index.keywords`
2. Include **related** specs referenced via `related_specs` / `related_plan` chains.
3. Record each file: path, `status`, `related_spec` / `related_plan`, GitHub issue refs in frontmatter.

### 4.2 Per-US grouping

For each user story in scope, list:

| US | Spec file(s) | Plan file(s) | GitHub epic/story issues (if any) | Plan tasks all `[x]`? |

Flag:

- US with **no spec** (PRD-only)
- Spec with **no plan** (acceptable if built ad hoc — note risk)
- Plan tasks **unchecked** while code appears shipped
- Duplicate plans for same story (version drift)

### 4.3 Plan ↔ spec traceability

For each plan, verify:

- `related_spec` path exists
- Traceability matrix / per-task “Spec story” covers all spec stories
- Verification matrix AC rows have test evidence in repo **or** marked manual-only

## Step 5: PRD & spec completeness audit

For **each US** in the epic:

### 5.1 PRD coverage

| Check | Action |
|-------|--------|
| Story text | Quote PRD row; confirm persona + outcome |
| Priority | Must vs Should — `--strict` elevates Should gaps |
| Linked FRs | From TRACEABILITY + spec `prd_refs` |
| Cross-epic deps | Note depends-on (e.g. US-04 → US-01–03) — verify integration points only, do not implement future US |

### 5.2 Spec story → implementation

For each spec **story** and **acceptance criterion**:

| Result | Meaning |
|--------|---------|
| ✅ Shipped | Code + tests prove AC |
| ⚠️ Partial | UI/API exists but AC edge case missing |
| ❌ Missing | No implementation |
| 🔮 Deferred | Explicitly out of spec Phase 1 / plan “Deferred work” — **not** a gap |
| 🚫 Future US | Belongs to another epic — **do not** recommend building now |

### 5.3 Open questions & risks

Carry forward unresolved items from spec/plan “Open questions” / “Risks” — mark **closed**, **still open**, or **obsolete**.

## Step 6: Synchronization audit

Verify alignment across layers. Record **drift** with file paths.

| Layer | Checks |
|-------|--------|
| **PRD ↔ spec** | No contradicting behavior; spec doesn’t expand scope beyond PRD without label |
| **Spec ↔ plan** | Plans don’t omit spec stories; plan deviations documented in plan body |
| **Plan ↔ code** | Files listed in plans exist; unlisted hotspots discovered → drift |
| **OpenAPI ↔ API** | Run or reason about `openapi-contract-sync`: routes, status codes (401/422/409), schemas |
| **TECHNICAL** | Handlers, aggregates, tables, Redis keys match docs |
| **Frontend ↔ API** | Generated types / client usage match contract; query keys invalidate correctly |
| **SignalR** | Event names and payloads if epic touches real-time |
| **Config** | `appsettings` / AppHost env vars documented in Tech §13 |

### 6.1 Memory artifacts hygiene

| Check | Action |
|-------|--------|
| `current-status.md` | Reflects epic completion accurately |
| `decisions.md` | Epic ADRs present; no duplicate/conflicting entries |
| `known-issues.md` | Findings either listed or explicitly new |
| `CHANGELOG.md` | Spec/plan entries exist for major artifacts |
| Spec `status` | `approved` vs `draft` vs reality |

## Step 7: Test coverage audit

Map **epic-level test matrix** (not coverage %).

### 7.1 By layer

| Level | Project | Epic expectation |
|-------|---------|------------------|
| Domain unit | `tests/Solution.Domain.UnitTests` | Every new/changed aggregate invariant |
| Matching unit | `tests/Solution.MatchingEngine.UnitTests` | Only if epic touches matching |
| API integration | `tests/Solution.Api.IntegrationTests` | Happy + primary failure paths per endpoint |
| Frontend | — | MVP: manual checklist only — note gaps, don’t add tests unless `--fix` |

### 7.2 Per US test evidence

Build a table:

| US | Spec AC (summary) | Domain tests | Integration tests | Manual UI | Gap |
|----|-------------------|--------------|-------------------|-----------|-----|

**Search strategy:**

- Grep test names and files for story keywords, route paths, error `type` URIs
- Read plan “Tests required” tables vs `tests/` tree
- If `--run-tests` (default when needed):  
  `dotnet test tests/Solution.Domain.UnitTests`  
  `dotnet test tests/Solution.Api.IntegrationTests`  
  (filter by fully qualified name when epic is narrow)

### 7.3 Cross-US integration scenarios

Define 3–8 **epic journey** scenarios (e.g. register → login → wallet → reset) and mark covered / partial / missing.

## Step 8: Code quality & refactor audit

Use `code-reviewer` mindset **scoped to epic files** (from plans + grep for US keywords).

### 8.1 Architecture & patterns

| Check | Violation examples |
|-------|-------------------|
| Clean Architecture | Domain references EF/ASP.NET; Api contains business rules |
| CQRS | Commands in query handlers; missing validators |
| Async matching | API blocks on match completion |
| PostgreSQL vs Redis | Authoritative data only in Redis |
| Naming | `Trading` prefix, abbreviations, XML doc comments (`core.mdc`) |
| `AssemblyReference` | Discovery uses entry assembly instead of `AssemblyReference.Assembly` |

### 8.2 Safe improvements (epic-only)

Recommend improvements only when:

- They **close a gap** for a US in this epic, **or**
- They are **internal quality** (rename, extract, dedupe) with **no behavior change**, **or**
- They fix **sync drift** (OpenAPI, error types)

**Forbidden without explicit user approval:**

- Features for **future US** (other epics)
- Multi-symbol, broker, outbox, horizontal scale (`core.mdc` out of scope)
- Broad rewrites unrelated to epic touchpoints
- Changing **Must** behavior that future specs might rely on differently

Tag each recommendation:

| Tag | Meaning |
|-----|---------|
| `[epic-gap]` | Missing AC for in-scope US |
| `[sync]` | Contract/docs mismatch |
| `[hygiene]` | Cleanup, no product change |
| `[deferred]` | Nice-to-have; safe to postpone |
| `[future-us]` | Do when US-XX ships — **do not implement now** |

### 8.3 Tinh gọn & cleanup

Look for:

- Duplicate handlers/clients/mappers across stories (merge candidates)
- Copy-paste test fixtures
- Orphaned plan tasks, abandoned feature flags, commented code
- Files **> 500 lines** (`core.mdc`) — split proposals
- Redundant spec/plan prose — **archive or consolidate** suggestions (markdown only)
- `bin/`, `obj/` accidentally tracked — warn, don’t commit

### 8.4 Refactor roadmap

Order findings:

1. **P0** — Correctness/security/sync breaks epic AC  
2. **P1** — Test gaps for Must stories  
3. **P2** — Hygiene / dedupe inside epic boundaries  
4. **P3** — Deferred / future-us  

Each item: **title**, **files**, **effort S/M/L**, **risk**, **depends on**.

## Step 9: Produce the report

Present in chat using this structure (Vietnamese or English per user message language):

---

```markdown
# Epic Review: <Epic name>

| Field | Value |
|-------|--------|
| User stories | US-01, US-02, … |
| Review date | <ISO-8601> |
| Artifacts | <N> specs, <M> plans (listed below) |
| Tests run | Yes / No — <summary> |
| Verdict | 🟢 Ready to close / 🟡 Close with follow-ups / 🔴 Not ready |

## Executive summary

<3–6 sentences: overall health, top risks, whether epic meets PRD §10 for its stories>

## Artifact inventory

| Path | Type | US | Status | Notes |
|------|------|-----|--------|-------|

## Completeness matrix (PRD + spec → code)

| US | Priority | Spec/plan | Verdict | Missing / partial items |
|----|----------|-----------|---------|-------------------------|

## Synchronization drift

| Area | Expected | Actual | Severity | Fix tag |
|------|----------|--------|----------|---------|

## Test coverage matrix

| US | Domain | Integration | Manual | Epic journeys |
|----|--------|-------------|--------|---------------|

## Recommendations

### Must fix before epic close (P0–P1)
- …

### Safe hygiene (P2)
- …

### Explicitly deferred / future US (do not do now)
- …

## Refactor roadmap

| Priority | Item | Files | Effort | Risk |
|----------|------|-------|--------|------|

## Artifact cleanup (docs only)

- Consolidate / archive / update status suggestions …

## Suggested next commands

- `/build` on plan `<path>` if tasks open  
- `/plan` if spec drift requires re-planning  
- `create-pr` if closing a hygiene branch  
```

---

### 9.1 Optional save (`--save`)

Write to: `docs/reviews/<YYYYMMDD-HHmmss>-<epic-kebab>.md`

Frontmatter (minimal):

```yaml
---
artifact_type: review
artifact_version: 1
id: review-<timestamp>-<epic-kebab>
title: Epic review — <Epic name>
created_at: <ISO-8601>
epic: <PRD §5 section title>
user_stories: [US-01, US-02]
specs: [docs/specs/...]
plans: [docs/plans/...]
verdict: ready | follow-ups | not-ready
tags: [epic-review, trading-simulator]
---
```

Append to [`docs/CHANGELOG.md`](docs/CHANGELOG.md): `- review: <epic> (docs/reviews/<file>.md)`

Update [`docs/memory/current-status.md`](docs/memory/current-status.md): epic review verdict + top P0/P1 bullets.

Add new confirmed bugs to [`docs/memory/known-issues.md`](docs/memory/known-issues.md) (not speculation).

Link consolidated paths in the review report when generated (see §Consolidated artifacts).

### 9.2 Epic archive (`--consolidate`)

> **Goal:** After an epic ships, replace scattered specs/plans with **durable epic archives** so a future reader knows **everything that was done** — without opening 20+ files.

**Output paths (canonical — authoritative after archive):**

| Output | Path |
|--------|------|
| Epic index | `docs/epics/<epic-kebab>/README.md` |
| Archived specs | `docs/epics/<epic-kebab>/specs.md` |
| Archived plans | `docs/epics/<epic-kebab>/plans.md` |

Example: `docs/epics/account-management/`

#### Two-part file structure (mandatory)

Each archive file has **Part 1 (readable epic record)** + **Part 2 (verbatim sources)**:

| Part | Specs file (`specs.md`) | Plans file (`plans.md`) |
|------|-------------------------|-------------------------|
| **Part 1** | PRD table, per-US “what shipped” (API, UX, BR summary), cross-epic deps, traceability | Plan index (18 rows), code surfaces, timeline by US, test class list, operator follow-ups |
| **Part 2** | Full text of every spec (frontmatter, stories, AC, edge cases) | Full text of every plan (tasks `[x]`/`[ ]`, files, tests, manual checklists) |

Part 1 must stand alone for onboarding (“what did this epic do?”). Part 2 preserves **zero content loss** for audit.

#### Consolidation rules (mandatory)

1. **Verbatim Part 2** — Copy full file content; no dropping sections. Preserve task checkboxes, GitHub URLs, code fences.
2. **Part 1 synthesis** — Derive from review matrix + specs/plans + code grep (endpoints, key paths, test counts, ADRs). Not a substitute for Part 2.
3. **Wrapper frontmatter** — `artifact_type: epic-archive-specs` / `epic-archive-plans`, `archived_at`, `sources_deleted: true|false`, `source_files:` (historical paths), `related_review:`.
4. **Per-source section** — `## Source N of M: \`docs/.../original.md\`` then entire file.
5. **Ordering** — Specs by US-01…; plans by US then story number (filename timestamp).
6. **`README.md`** — Short epic summary + links to `specs.md`, `plans.md`, review file.

#### Delete sources (default)

After verifying Part 2 contains all sources (byte-count or section count checklist):

1. Delete every epic spec under `docs/specs/` listed in `source_files`
2. Delete every epic plan under `docs/plans/` listed in `source_files`
3. Set `sources_deleted: true` in archive frontmatter

Skip deletion only with `--keep-sources` (e.g. mid-epic work still in flight).

**Pre-delete checklist:**

| # | Check |
|---|--------|
| 1 | Part 2 section count = `source_count` |
| 2 | Part 1 lists every US and plan index row |
| 3 | Epic review report saved (if `--save`) |
| 4 | User did not pass `--keep-sources` |

#### Changelog

Append: `- epic-archived: <epic> (docs/epics/<epic-kebab>/; N specs + M plans merged, sources deleted)`

Update [`docs/memory/current-status.md`](docs/memory/current-status.md): point active epic docs to `docs/epics/<epic-kebab>/README.md`.

#### Report linkage

```markdown
## Epic archive

| File | Role |
|------|------|
| `docs/epics/<epic-kebab>/README.md` | Start here |
| `docs/epics/<epic-kebab>/specs.md` | Part 1 product record + Part 2 verbatim specs |
| `docs/epics/<epic-kebab>/plans.md` | Part 1 implementation record + Part 2 verbatim plans |

Sources under `docs/specs/` and `docs/plans/` for this epic: **deleted** (or retained if `--keep-sources`).
```

## Step 10: Optional fix pass (`--fix`)

Only after the report and **explicit user approval** (`AskQuestion`: which P0/P1 items to implement):

1. Prefer **smallest diffs**: sync fixes, missing tests, OpenAPI export, doc status updates
2. One logical commit worth per approval batch
3. Re-run tests touched
4. Update plan checkboxes / spec status if behavior now matches
5. **Never** implement `[future-us]` items

If user did not pass `--fix`, end with: “Say which P0/P1 items to fix and re-run with `--fix`.”

## Step 11: Quality checklist (self-score before presenting)

| # | Criterion |
|---|-----------|
| 1 | Every in-scope US appears in completeness matrix |
| 2 | All discovered specs/plans listed or explained absent |
| 3 | Future US clearly separated from gaps |
| 4 | Test matrix cites real test classes/methods or states “none” |
| 5 | Sync drift cites concrete paths (OpenAPI, handlers, components) |
| 6 | Refactor items ordered P0→P3 with tags |
| 7 | No recommendation violates `core.mdc` out-of-scope |
| 8 | `--run-tests` results reflected if executed |
| 9 | Verdict matches P0 count (🔴 if any Must AC missing) |
| 10 | User can act without re-reading all plans |
| 11 | If `--consolidate`: every source spec/plan appears verbatim once in merged files |

## Guidelines

**DO:** Read every epic spec/plan · Trace AC to code and tests · Cite `PRD §`, `Tech §` · Respect ADRs · Parallelize discovery reads · Use `AskQuestion` for epic/US ambiguity · Tag future work explicitly

**DON'T:** Implement features for unbuilt US · Expand MVP scope · Auto-commit · Close GitHub issues · Treat deferred spec scope as bugs · Run destructive git commands · Skip skills · Give generic advice without file evidence

## Example invocations

```text
/epic-review Account Management
/epic-review --us US-01,US-02,US-03,US-04 --save --run-tests
/epic-review Market Data --strict --no-tests
/epic-review Account Management --fix
/epic-review Account Management --save --consolidate
/epic-review Account Management --consolidate
/epic-review Account Management --consolidate --keep-sources
```
