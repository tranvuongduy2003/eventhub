---
name: cook
description: "Run the complete EventHub idea-to-implementation loop: brainstorm from a user idea or docs/features.md target, create or refine a committed docs/specs implementation spec, produce a detailed implementation plan, implement in small feature slices, test, review, verify, and prepare an optional PR handoff."
---

# Cook - idea to spec to implementation

Run from the repository root. `$cook` starts from an idea, product direction, existing spec, or
target feature from `docs/features.md`, for example: "implement one planned feature from
`docs/features.md`." Codex must use strong reasoning for product discovery, specification,
planning, review, and verification, then use medium reasoning for implementation/test-writing speed
once the spec and plan are explicit.

The durable output of the discovery phase is a committed implementation spec in `docs/specs/`.
Implementation work proceeds only after that spec is complete enough to serve as the contract.

## Model routing

- **Strong / high reasoning:** `requirement-analyst`, `spec-brainstormer`,
  `implementation-planner`, `code-reviewer`, `security-reviewer`, `acceptance-verifier`, and
  `harness-doctor`. Use these for ambiguity removal, product synthesis, planning, and gates.
- **Medium reasoning:** `implementer` and `test-writer`. Use these after the spec and plan have
  narrowed the solution space.
- If a caller cannot select model families directly, enforce this split through the subagent
  `model_reasoning_effort` settings and by delegating only the right phase to each subagent.

## Input

`$ARGUMENTS` may be any of:

- a raw product idea;
- a feature id or epic from `docs/features.md`;
- a request to choose the next valuable planned feature;
- an existing `docs/specs/<file>.md` path to implement or refresh;
- issue text or PR comments, only when the user explicitly supplies or requests them.

If empty, inspect `docs/features.md` and `docs/specs/` to choose the next planned or
not-confirmed feature with the clearest dependency chain. Do not require an issue id.

## Flow

```text
$cook <idea-or-feature>
  -> source grounding
  -> requirement-analyst clarity gate
  -> scope narrowing when only a subset is ready
  -> spec-brainstormer writes/refines docs/specs/<timestamp>-<slug>.md
  -> implementation-planner writes .codex/tmp/implementation-plan.md
  -> branch
  -> loop-init creates .codex/tmp/features.json + .codex/tmp/codex-progress.md
  -> loop-next until done
  -> final review + acceptance verification
  -> optional draft PR handoff only when explicitly approved/requested
```

## Step 1 - Ground the idea

Read the smallest relevant source set before synthesis:

1. `docs/product.md`, `docs/features.md`, and `docs/technical.md`.
2. `docs/specs/README.md` and nearby specs for adjacent or prerequisite features.
3. Root `AGENTS.md` and any nested `AGENTS.md` for likely touched areas.
4. Current source code only enough to know what already exists and what is missing.

Capture the input as an **idea brief**, not as a final requirement. Include the feature/epic ids,
personas, existing constraints, likely dependencies, and unknowns.

## Step 2 - Clarity gate

Run `requirement-analyst` on the idea brief and grounded source. It checks whether enough product,
technical, and dependency context exists to brainstorm a spec without inventing behavior that
conflicts with `docs/`.

- **NEEDS-CLARIFICATION** -> write the report to `.codex/tmp/<slug>-clarify.md` and stop. Ask the
  user only the blocking questions whose answers would change product behavior, security,
  architecture, data shape, or scope.
- **CLEAR_FOR_SUBSET** -> continue only for the ready subset named by `requirement-analyst`. Record
  the excluded feature ids/behaviors in the idea brief passed to `spec-brainstormer`, and require
  the spec to put them in **Out of Scope** or **Risks and Follow-up Decisions**. Do not create a
  branch yet; the scoped spec still comes first.
- **CLEAR** -> continue. Do not create a branch yet; the spec comes first.

When a target epic mixes `Next` and `Later` features, implemented and planned dependencies, or
independent workflows with different missing decisions, prefer a scoped first spec over blocking the
whole epic. The scope is valid only when it is source-backed, independently valuable, and its
deferred behavior does not need to be invented to implement the ready behavior.

## Step 3 - Brainstorm and write the implementation spec

Run `spec-brainstormer` with strong reasoning. It must create or refine one implementation-ready
spec under `docs/specs/` using the naming rule from `docs/specs/README.md`:

```text
docs/specs/<YYYYMMDDHHmmss>-<feature-kebab>.md
```

The spec must be detailed enough that implementation can be delegated to a cheaper model without
product guessing. It should include, as applicable:

- frontmatter with `doc_schema`, `doc_kind: implementation_spec`, `doc_id`, `title`, `status`,
  `last_updated`, `owner`, `language`, and `applies_to`;
- problem, solution, in-scope, out-of-scope, personas, dependencies, and source links;
- acceptance criteria in verifiable GIVEN/WHEN/THEN form;
- business rules, invariants, authorization, security, sensitive-data, and audit requirements;
- API, contract, data, integration, frontend, e2e, and observability expectations;
- edge cases, failure modes, concurrency/idempotency concerns, test strategy, risks, and
  implementation notes;
- explicit "not decided" questions only when they do not block the first implementation slice.

If an existing spec is stale or thin, refine it rather than creating a competing spec. Do not make
`docs/specs/` a second source of truth: reconcile durable behavior back to `docs/features.md` or
`docs/technical.md` when the brainstorm reveals source-spec drift.

For a `CLEAR_FOR_SUBSET` input, create or refine exactly one spec for the ready subset. Name the
spec after that subset rather than the whole epic, keep deferred features out of implementation
scope, and include the prerequisite questions that must be answered before a later spec can cover
the excluded work.

## Step 4 - Plan from the spec

Run `implementation-planner` with strong reasoning. It reads the new or selected spec and produces
`.codex/tmp/implementation-plan.md` with:

- implementation slices in dependency order;
- an Area Coverage Matrix that classifies backend, contracts, web, and e2e obligations from the
  spec and states which slice and sensor proves each required area;
- intended files/areas and path-scoped `AGENTS.md` files;
- domain/application/infrastructure/api/contracts/web/e2e impacts;
- OpenAPI/codegen needs;
- test and validation commands;
- invariants and risk tier per slice;
- rollback point from `git rev-parse HEAD`;
- live-browser verification needs, when UI behavior cannot be proven from tests alone.

The plan must be detailed and explicit; implementation agents should not need to invent endpoint
shapes, DTO names, state transitions, or acceptance evidence.

Frontend and e2e coverage are first-class obligations. If the spec or source docs require UI,
browser, route, cache, cross-page, or live-user workflow behavior, the plan must include dedicated
web and/or e2e slices instead of treating backend completion or final verification as a substitute.

## Step 5 - Create the feature branch

After the spec and plan exist:

- If already on a dedicated feature branch for this spec, stay on it.
- Otherwise create `feature/<spec-slug>` from the current base without prompting.
- Do not open, update, or merge a PR unless the user explicitly approved that GitHub operation.

## Step 6 - Initialize the coding loop

Run `$loop-init` using the spec and `.codex/tmp/implementation-plan.md`. It creates:

- `.codex/tmp/features.json`
- `.codex/tmp/codex-progress.md`

Each feature slice is a contract: allowed files, acceptance criteria, invariants, validation
commands, risk tier, and convergence criteria.

`features.json` must preserve the plan's area coverage. Required `web` and `e2e` work must remain
real pending slices until implemented, explicitly blocked, or excluded for a source-backed reason.
A generic final verification slice must not replace planned frontend or Playwright work.

## Step 7 - Implement each slice

Run `$loop-next` until every slice in `features.json` is `done`. `loop-next` owns the per-slice
implementation loop:

- delegate implementation to `implementer` using medium reasoning;
- delegate focused tests to `test-writer` using medium reasoning;
- run validation commands and deterministic sensors;
- run `code-reviewer` always and `security-reviewer` when relevant, both strong reasoning;
- run `acceptance-verifier` as a hard gate against the spec and slice criteria;
- fix and re-verify until PASS.

Do not skip review/verification because the spec looked good. The spec reduces ambiguity; sensors
still decide completion.

## Step 8 - Final handoff

Before handoff, run changed-code verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

If the user explicitly requested a PR, use `$create-pr` only after approval and after the final
verification gate is green. Otherwise stop with a concise local handoff: spec path, plan path,
changed files, checks run, acceptance status, and residual risk.

## Behavior summary

- The default workflow starts from an idea or `docs/features.md`, not from GitHub Issues.
- A complete `docs/specs/` implementation spec is mandatory before coding.
- Strong models handle ambiguity, spec, planning, review, and verification.
- Medium models handle implementation and test writing after the contract is explicit.
- PR operations are optional and approval-gated.
