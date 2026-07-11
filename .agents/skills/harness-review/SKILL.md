---
name: harness-review
description: Review Codex harness telemetry, agents, skills, hooks, rules, evals, and propose governed improvements.
---

---
description: Diagnose the agent harness itself from deep telemetry - run the harness-doctor (Evolution Agent), write a dated review report, and gate every proposed change behind the regression suite and HITL. Read-only diagnosis; a human applies changes.
argument-hint: (optional) a focus area, e.g. "permissions" or "loop"
---

# Harness Review - Agentic Harness Engineering (AHE)

Measure and improve the **harness itself**, not the Solution product (Agentic Harness Engineering).
This command runs the read-only `harness-doctor` Evolution Agent over the deep telemetry and harness
configuration, produces a dated report, and enforces **governed mutation**: nothing is applied
automatically - a human reviews, and the regression suite plus HITL gate every change.

**Input:** `$ARGUMENTS` - optional focus area (e.g. `permissions`, `loop`, `sensors`). If empty, do a
full review.

## Step 1 - Confirm there is telemetry to analyze

Check `.codex/tmp/telemetry/` for `*.jsonl` files. If none exist yet, tell the user the harness has
not accumulated traces - run a few `$cook` sessions first - and stop. (Telemetry is gitignored scratch;
it only exists after real sessions in this working tree.)

## Step 2 - Run the Evolution Agent (read-only)

Delegate to the `harness-doctor` subagent. It reads `.codex/tmp/telemetry/*.jsonl` plus the harness
config (`.codex/{agents,skills,hooks,rules,commands}`, `settings.json`, `AGENTS.md`,
`docs/harness/*`) and returns diagnosed failure modes + proposed revisions + harness-level metrics.
Pass `$ARGUMENTS` as the focus if provided.

## Step 3 - Write the report (do not edit the harness)

Write the agent's full report to **`.codex/tmp/harness-review-<YYYY-MM-DD>.md`** (gitignored scratch).
Do **not** apply any proposal in this step. This command is diagnosis + proposal only (human decision
#2 - `harness-doctor` is read-only).

## Step 4 - Governed mutation rules - apply ONLY after human approval

Any harness change that comes out of this review must, before it is activated:

1. **Be evaluated against the current harness sensors** in `scripts/agent/` - at minimum docs
   validation, harness-policy validation, and changed-code verification when the change affects code.
2. **Carry an auditable rationale** - the report entry (evidence -> root cause -> change) is that record.
3. **Go through HITL if it touches a boundary** - any change to permission / network / credential /
   deploy / human-review rules (`settings.json` permissions, `guard-dangerous.ps1`) requires explicit
   human approval and is **never** auto-applied.

The Evolution Agent is itself subject to PEV: a proposed mutation is a plan, applying it is execution,
and the regression suite is its verification. **`harness-doctor` never edits the harness** - a human,
or `implementer` after the human approves, makes the edit and then re-runs the regression suite.

## Step 5 - Apply the standing caveats

Frame every proposal against the permanent limits in `docs/harness/caveats.md`: green != correct
(oracle adequacy), self-evolving must not regress, HITL is harness state, verify beyond execution
feedback, and the 3"6-month / post-model-upgrade review cadence. A proposal that would weaken any of
these (e.g. loosen a permission gate, drop a semantic-review layer) is HITL-required and must be
argued explicitly, never slipped in as an "efficiency" win.

## Output

Tell the user: the report path, the top diagnosed failure modes, the **harness-level metrics**
(trajectory efficiency, verification strength, recovery, state consistency, safety compliance,
replayability), and the recommended proposals - each marked with its motivating evidence, the
harness-eval scenario that would validate it, and whether it is HITL-required. Then stop; wait for the
human to choose what to apply.








