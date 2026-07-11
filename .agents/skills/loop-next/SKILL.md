---
name: loop-next
description: >
  Advance the agentic coding loop by implementing exactly ONE feature from the feature list, then
  leaving a clean state (green build + commit + updated progress) so the next session can continue
  safely. Use after loop-init, when the user says "next feature", "continue the loop", or
  "/loop-next", or as the per-feature step of $cook. A feature is marked done only after the
  acceptance gate (acceptance-verifier) returns PASS - clean state means matching the spec, not
  just a green build.
---

# Loop Next

Do one spec slice well and stop clean. This avoids the two classic failures: running out of context
mid-slice, and declaring "done" while the work is incomplete or drifts from the spec.

## Steps

1. **Re-sync to ground truth FIRST (anti belief-divergence).** Before trusting any progress
   note, read the _real_ state of the working tree - the progress file is a _belief_, git + tests are
   the _ground truth_, and the two can drift. Run `git status` and `git diff --stat HEAD`,
   check the current branch is the feature branch (never `main`), and skim the files in the
   in-progress feature's `filesTouched`. If the working tree contradicts `codex-progress.md` (e.g.
   progress says a feature is `done` but the code is absent, or there are uncommitted leftovers),
   **believe git, not the note** - reconcile the artifacts to match reality before continuing.
2. **Read the artifacts.** Load the selected `docs/specs/<...>.md`,
   `.codex/tmp/implementation-plan.md` when present, `.codex/tmp/features.json`, and
   `.codex/tmp/codex-progress.md`.
   Pick the **first feature with status `pending`** (or resume the one marked `in-progress`). If none
   remain `pending`/`in-progress`, report that the loop is complete and stop.
3. **Mark it `in-progress`** in `features.json` and `codex-progress.md`.
4. **Implement the single slice.** Delegate to the medium-reasoning `implementer` subagent (it reads
   the spec, plan, path-scoped rules, and the `backend`/`web`/`e2e` skill for the area it touches).
   Keep strictly to this one slice - do not start the next.
5. **Write and run tests, then run the feature's `validationCommands`.** Delegate to `test-writer`
   for the tests, then run them yourself: backend `dotnet test`; web/e2e the relevant `yarn`
   command. In addition, run every command in this feature's `validationCommands` array from
   `features.json` - those are the deterministic sensors the contract says prove _this_ feature. Fix
   until all are green (100% backend coverage is enforced - use `$backend` -> coverage workflow
   and `powershell -NoProfile -ExecutionPolicy Bypass -File ps/Get-CodeCoverage.ps1`). Also confirm the feature's `invariants` still hold and that
   edits stayed within `filesTouched` - a touched file outside that list is scope drift: either it
   belongs to this feature (update the contract) or it does not (revert it).
6. **Review.** Run strong-reasoning `code-reviewer` (always) and `security-reviewer` (when the
   feature touches auth, payment/record data, sensitive user data, or external integrations).
   Address must-fix findings.
7. **Acceptance gate (hard).** Run strong-reasoning `acceptance-verifier` against the selected spec
   and this slice's acceptance criteria.
   - **PASS** -> continue to step 8.
   - **FAIL** -> go back to step 4, fix the missing/divergent/extra behavior, and re-verify. Do NOT
     mark the feature done while any FAIL remains.
8. **Leave a clean state:**
   - Confirm the build is green.
   - Set the feature `status` to `done` in `features.json`; check its box in `codex-progress.md` and
     append a one-line log entry. These live in gitignored `.codex/tmp/` - updated on disk, not
     committed.
   - **Record the evidence bundle** for this feature into `codex-progress.md` (from the
     `acceptance-verifier` output): the sensors that ran and their results, what was **not** verified
     and the residual risk, and the invariants confirmed. This is mandatory even when everything
     passed - see `docs/harness/evidence-bundle.md`.
   - Commit the **feature code** (on the feature branch, never `main`): `git add -A` then a commit
     named `feat: <feature title>`. The progress artifacts are skipped automatically because they
     are gitignored. Do not include external work-item ids unless the spec explicitly came from one.
9. **Report** which feature was completed, the verdicts, and how many features remain. Stop - one
   feature per invocation.

## Convergence criteria (explicit - never implicit)

A feature converges on **objective evidence**, not on a feeling that enough rounds have run. This is
the difference between test-gated convergence and _implicit convergence_ (the weakest, brittlest
kind). A feature is `done` only when ALL of these hold:

- every command in the feature's `validationCommands` exits green;
- backend coverage gate is satisfied (100% on `Solution.Api`);
- `code-reviewer` (and `security-reviewer` when applicable) must-fix findings are resolved;
- `acceptance-verifier` returns **PASS** for every spec/slice acceptance criterion;
- the feature's `invariants` still hold and edits stayed within `filesTouched`;
- progress artifacts are updated on disk and a feature-code commit is made.

**Do NOT mark a feature done because it "feels done", because the loop should advance, or because a
cheap sensor (build/lint) is green while an expensive one (tests/coverage/acceptance) was skipped.**
"Green build" alone is not "done" - matching the spec, proven by the contract's sensors, is the bar.

## Notes

- If context runs low mid-feature, still leave the tree buildable and record exactly what remains in
  the feature's `notes` and the progress log, then stop. The next `loop-next` resumes from there.
- Never mark a feature done to make the loop advance - an unfinished feature stays `in-progress`.
- If you fan out to multiple agents (the `Workflow` tool) for a feature, follow
  `docs/harness/shared-substrate.md`: ground shared state in git worktree + tests (not text),
  keep the topology simple, and converge only on objective signals.
