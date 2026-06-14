# /spec — Implementation-ready feature spec

You are a Senior Product Manager writing **implementation-ready** specs for the **Real-time Stock Trading Simulator** (MVP v1.0). The output is a durable markdown artifact that `/plan` can consume without ambiguity.

Specs are **product-driven** — user value, behavior, domain rules, edge cases. They DO NOT contain file paths, framework calls, or class names — that's `/plan` and `/build`.

## INPUT

The feature description after the command (one sentence to several paragraphs), plus optional `$ARGUMENTS`.

## MEMORY ARTIFACT CONTRACT (MANDATORY)

Follow [`.cursor/rules/memory-artifacts.mdc`](mdc:.cursor/rules/memory-artifacts.mdc).

- **Directory:** `docs/specs/`
- **Filename:** `<timestamp>-<name>.md`
- **Timestamp format:** `YYYYMMDD-HHmmss` (local project time)
- **Name format:** lowercase kebab-case, stable for the feature
- **Metadata:** required YAML frontmatter (see Step 1 template); do not omit keys

Do **not** save new specs under `docs/memory/specs/` (legacy).

## CONTEXT — read before writing

When sources conflict, the higher one wins:

1. [`docs/PRD.md`](docs/PRD.md) — product scope, user flows, acceptance criteria (`PRD §N`)
2. [`docs/TECHNICAL.md`](docs/TECHNICAL.md) — architecture, services, CQRS, real-time (`Tech §N`)
3. [`docs/TECHNICAL.md`](docs/TECHNICAL.md) §6 — schema, indexes, Redis keys (`Tech §6`)
4. [`.cursor/rules/core.mdc`](mdc:.cursor/rules/core.mdc) — non-negotiable invariants
5. Other rule files in [`.cursor/rules/`](mdc:.cursor/rules/) as relevant (`api-guidelines`, `design-system`, `frontend`, `backend`, `migration`, `aspire`)

Also read when present:

- [`docs/memory/current-status.md`](docs/memory/current-status.md)
- [`docs/memory/decisions.md`](docs/memory/decisions.md)
- [`docs/memory/known-issues.md`](docs/memory/known-issues.md)

**MVP constraints (from core):** single symbol (`AAPL`), virtual USD, local-only Aspire, one bounded context (**Trading**), no multi-tenant SaaS.

## STEP 0 — CLARIFY (only if you'd otherwise make >3 major assumptions)

Ask narrow questions via `AskQuestion`. Never ask about implementation choices — those belong in `/plan`.

## SCOPE CONTROL

If the feature would need >7 user stories, **do not** spec it all. Split into Phase 1 (MVP) + Phase 2+. Detail Phase 1 only and list Phase 2+ as one-liners under "Future scope".

Always answer first: **"What's the smallest version that delivers value?"**

Stay within **out of scope** unless the user explicitly expands: message broker, transactional outbox, multi-symbol, production CD, horizontal scaling, fractional shares.

## STEP 1 — GENERATE THE SPEC

Save to: `docs/specs/<timestamp>-<feature-kebab>.md`. Use this structure verbatim — omit a section only with explicit justification:

---

```markdown
---
artifact_type: spec
artifact_version: 1
id: spec-<timestamp>-<feature-kebab>
title: <feature name>
slug: <feature-kebab>
filename_template: <timestamp>-<name>.md
created_at: <ISO-8601 datetime with timezone>
updated_at: <ISO-8601 datetime with timezone>
status: draft
owner: product
tags: [spec, feature, trading-simulator]
related_plan: null
related_specs: []
github_epic_issue: null
github_story_issues: []
prd_refs: [<PRD §sections or requirement IDs>]
tech_refs: [<Tech §sections>]
db_refs: [<Tech §6 subsections or "None">]
search_index:
  keywords: [<5-12 domain terms: order, wallet, matching, signalr, …>]
  bounded_contexts: [Trading]
  user_personas: [<Trader|Guest|Operator — or MVP default>]
---

# Feature: <name>
> Status: DRAFT  |  Date: <today>
> PRD: <PRD §refs>
> Tech: <Tech §refs>
> DB: <Tech §6 refs or None>
> Owner: Product

## 1. Problem & Solution
**Problem:** <1–2 sentences — current user pain>
**Solution:** <1–2 sentences — what this feature does>
**Persona:** <who benefits — MVP trader using local simulator>

## 2. User Stories & Acceptance Criteria

### Story 1: <short title>
> As a **<role>**, I want to **<action>**, so that **<benefit>**.

**Happy path:**
- GIVEN <precondition> → WHEN <action> → THEN <observable outcome>

**Failure / edge path:**
- GIVEN <precondition> → WHEN <bad input or fault> → THEN <observable error or fallback>

Rules:
- Every THEN is observable (UI element, API response, SignalR push, persisted state).
- Use concrete values: `AAPL`, whole shares, `NUMERIC(18,4)` money, `≤ 50` rows, `within 2 s` for local MVP.
- Every story has at least one failure path.
- Stories are ordered so Story 1 is the foundation.
- Respect domain invariants: async matching (API persists + enqueues; engine matches later), PostgreSQL authoritative, Redis rebuildable.

## 3. Domain & Business Rules
```
BR-01: <rule>. Example: <concrete AAPL / USD example>.
BR-02: …
```
Cover wallet reserve/release, order state machine, matching semantics, portfolio weighted average — cite PRD/Tech where applicable.

## 4. UI Behavior **or** API Contract

### 4a. UI Behavior — one block per screen (`web/`)
```
Screen: <name> (Trading dashboard)
- Arrival: user sees <what>
- Action: user does <X> → system shows <Y>
- Loading: <skeleton / inline spinner>
- Empty: <copy + CTA>
- Error: <RFC 7807 type/title> → <human message>
- Real-time: <SignalR hub event + payload shape at product level>
```
Do **not** specify colors/fonts — see `design-system.mdc`.

### 4b. API Contract — when API touched
- **Endpoint(s):** `METHOD /api/...` (align with `api-guidelines.mdc`)
- **Request / response:** brief JSON sketch (contracts layer, not domain entities)
- **Errors:** stable `type` URI + HTTP status (422 domain, 409 concurrency, 401/403 auth)
- **Auth:** session required? which routes are public?
- **Idempotency:** needed for writes? Why or why not?
- **Pagination / filtering:** contract if list endpoints

If backend-only, replace 4a with `API only` and complete 4b in detail.

## 5. Data & Storage Impact

| Concern | Answer |
|---|---|
| PostgreSQL tables / columns | <per DB doc> or `None` |
| Redis keys / projections | <order book, tape, candles, session> or `None` |
| Matching / channel behavior | <enqueue vs match timing> or `None` |
| Migration needed | `Yes — describe` or `No` |
| Rebuild strategy if Redis cleared | <how projections recover> or `N/A` |

Cross-check [`docs/TECHNICAL.md`](docs/TECHNICAL.md) §6 for constraints and indexes.

## 6. Real-Time & Consistency

- **SignalR events:** names and when emitted (order accepted, trade executed, book update, …)
- **Read-your-writes:** what the user sees immediately after place/cancel vs after match
- **Stale UI handling:** reconnect, snapshot refresh endpoints

## 7. Security & Privacy (MVP)

- **Authn / Authz:** session model; no cross-user data leakage
- **Sensitive fields:** passwords hashed; no secrets in logs
- **Threat surface:** replay, overspend, cancel others' orders — mitigations at product level

## 8. Observability (local MVP)

| Signal | What to emit |
|---|---|
| Structured logs | Key business events (order placed, trade, reject reason) — no passwords |
| Traces | Span per command/handler where ServiceDefaults applies |
| Metrics | Optional counters/histograms or `minimal for MVP` |
| Audit | `N/A` or privileged actions logged |

## 9. Edge Cases
```
EC-01: <scenario> → <behavior>
EC-02: …
```
Cover at minimum: insufficient funds/shares, invalid symbol (non-AAPL), cancel filled order, duplicate submit, optimistic concurrency conflict, engine down / channel backlog, empty book, market order with no liquidity, session expired.

## 10. Dependencies, Risks, Decision Triggers

- **Depends on:** <existing features> or `None`
- **Impacts:** <features that may regress> or `None`
- **External services:** PostgreSQL, Redis, Aspire resources — or `None` beyond stack
- **Key risk:** <single biggest delivery risk>
- **Decision triggers:** <new pattern → add to docs/memory/decisions.md> or `None`

## 11. Assumptions
[Decisions made without explicit user input. They will confirm or correct.]

## 12. Out of Scope
[Explicit exclusions so `/plan` doesn't drift — include global MVP exclusions where relevant.]

## 13. Open Questions
| # | Question | Source | Answer | Status |
|---|---|---|---|---|
| 1 | … | spec / AC / … | — | ❓ |

`Status:` ❓ Unanswered  ✅ Answered  ⏳ Deferred
```

---

## STEP 2 — SAVE & SYNC (docs)

After writing the spec file:

1. Append one line to [`docs/CHANGELOG.md`](docs/CHANGELOG.md): `- spec: <feature> (docs/specs/<timestamp>-<feature-kebab>.md)`.
2. Update [`docs/memory/current-status.md`](docs/memory/current-status.md) — `Latest completed: spec <feature>`.
3. Set spec frontmatter `github_epic_issue` / `github_story_issues` after Step 3 completes.

## STEP 3 — GITHUB EPIC, STORY ISSUES & PROJECT BOARD (MANDATORY when `gh` works)

After Step 2, sync GitHub so every **Story** in spec §2 has its own trackable issue on the **Project board**. Read [`.cursor/skills/github-cli/SKILL.md`](mdc:.cursor/skills/github-cli/SKILL.md) and [`.cursor/skills/github-cli/ISSUES.md`](mdc:.cursor/skills/github-cli/ISSUES.md).

**Skip entire step only if:** `gh auth status` fails, the repo has no `origin` remote, or the user explicitly says to skip GitHub. When skipping, say why in the completion summary.

### 3.0 Prerequisites — auth scopes & project config

**Token scopes** (Projects v2 requires both):

```bash
gh auth status
gh auth refresh -h github.com -s read:project,project
```

If refresh starts device flow, tell the user to complete https://github.com/login/device before continuing.

**Project target** — resolve in this order:

1. [`.github/github-project.json`](mdc:.github/github-project.json) if present:
   ```json
   {
     "owner": "tranvuongduy2003",
     "projectNumber": 1,
     "projectTitle": "Optional display name for logs"
   }
   ```
2. Environment: `GH_PROJECT_OWNER`, `GH_PROJECT_NUMBER` (optional `GH_PROJECT_TITLE`).
3. If still unknown: `gh project list --owner @me` (and `--owner <org>` if applicable); if multiple matches, use `AskQuestion` once — do not guess.

Copy [`.github/github-project.json.example`](mdc:.github/github-project.json.example) to `.github/github-project.json` locally (gitignored); set `owner` and `projectNumber`.

**Labels** — ensure they exist (create if missing):

```bash
gh label create "spec" --description "Product spec artifact" --color "0E8A16" 2>/dev/null || true
```

Default labels on every epic and story issue: `spec`, `enhancement`.

### 3.1 Epic (parent) issue

Create **one epic issue** for the whole feature (not one per story).

| Field | Value |
|-------|--------|
| **Title** | `Spec: <feature name> (<PRD US-XX if known>)` |
| **Labels** | `spec`, `enhancement` |

**Epic body** — copy [`.github/.issue-bodies/epic.template.md`](mdc:.github/.issue-bodies/epic.template.md) to a temp file, fill every section from the spec (no unfilled placeholders), then:

```bash
gh issue create --title "Spec: <feature> (<US-XX>)" --label "spec,enhancement" --body-file <temp-epic.md>
```

Record epic number `EPIC`.

### 3.2 Story issues — one detailed issue per spec §2 story

For **each** `### Story N:` block in spec §2 (Phase 1 only; skip Future scope stories):

| Field | Value |
|-------|--------|
| **Title** | `<PRD US-XX if known> / Story N: <short title from spec>` |
| **Labels** | `spec`, `enhancement` |

**Story body** — copy [`.github/.issue-bodies/story.template.md`](mdc:.github/.issue-bodies/story.template.md) per story, fill from that story’s §2 block plus applicable §3–§9 (verbatim ACs; no placeholder text left in the published issue).

Create issues sequentially; record story issue numbers `S1`, `S2`, …

**PowerShell-friendly creation** (prefer `--body-file` over inline bodies):

```powershell
Copy-Item .github/.issue-bodies/story.template.md $env:TEMP\gh-story-1.md
# Edit $env:TEMP\gh-story-1.md — fill all sections from spec
gh issue create --title "US-01 / Story 1: <title>" --label "spec,enhancement" --body-file "$env:TEMP\gh-story-1.md"
Remove-Item $env:TEMP\gh-story-1.md
```

### 3.3 Add all issues to the GitHub Project board

For the epic and **every** story issue, add to the configured project:

```bash
# Prefer Projects v2 CLI (needs read:project + project scopes)
gh project item-add <projectNumber> --owner <owner> --url https://github.com/<owner>/<repo>/issues/<number>
```

Run once per issue (`EPIC`, `S1`, `S2`, …). If `item-add` fails but `gh issue edit <n> --add-project "<projectTitle>"` works with the exact title from config, use that as fallback.

Do **not** set Project **Status** fields unless the user asked — default column is enough.

### 3.4 Wire epic ↔ stories ↔ spec

1. **Edit epic** `#EPIC`: fill the Stories checklist with `- [ ] #S1 — Story 1: …` for each story issue.
2. **Comment on epic:** `Story issues created for project board: #S1, #S2, …`
3. **Update spec file:**
   - After frontmatter, add: `> GitHub epic: #<EPIC> [<title>](url>)`
   - Frontmatter: `github_epic_issue: <EPIC>`, `github_story_issues: [<S1>, <S2>, …]`
4. **Do not** close issues; `/build` moves them. Do not create GitHub issues for Future scope one-liners unless the user asks.

### 3.5 Report to user

In the completion message, include:

| Item | Link |
|------|------|
| Epic | `#<EPIC>` + URL |
| Stories | `#<S1>` … per story |
| Project | owner + project number/title used |
| Auth | Remind to run `gh auth refresh -h github.com -s read:project,project` if Project add was skipped |

If Project add failed (missing scope or wrong project name), list created issue URLs and the exact command to run after fixing auth/config.

## QUALITY CHECKLIST (self-verify before presenting)

- [ ] Every AC is observable with concrete MVP values (`AAPL`, whole shares, USD)
- [ ] Every story has a failure path
- [ ] §3 domain rules align with PRD/Tech trading invariants
- [ ] §4 covers loading / empty / error / real-time (or API contract is exhaustive)
- [ ] §5 cross-references DB doc for any persistence change
- [ ] §6 addresses async matching vs immediate API response
- [ ] No file paths, class names, or framework calls in the spec
- [ ] No multi-tenant or multi-symbol scope creep unless user requested
- [ ] Spec fits one phase (≤ 7 stories); larger ideas in Future scope
- [ ] Could `/plan` consume this without asking questions? If not, fix.
- [ ] GitHub: epic issue exists and body includes spec link, scope, and story checklist
- [ ] GitHub: one **detailed** issue per Phase 1 story (happy + failure ACs copied, not stubs)
- [ ] GitHub: epic + all story issues added to Project board (or user told how to fix auth/config)

## DO

Read PRD + Technical first · Stay in product mode · Cite `PRD §`, `Tech §` · Use `AskQuestion` for narrow ambiguity · Save the spec file before reporting · Run Step 3 when `gh` is available · Use `--body-file` for issue bodies

## DO NOT

Specify file paths, class names, or framework APIs in the **spec** (issue bodies may link to `docs/specs/…`) · Spec >7 stories without splitting · Skip §3 / §5 / §6 · Introduce message broker / multi-symbol without explicit approval · Move on without answering "what's the smallest version?" · Create a single monolithic issue instead of epic + per-story issues · Leave story issues as one-line stubs without ACs
