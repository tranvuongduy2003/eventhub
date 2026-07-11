---
name: loop-init
description: >
  Initialize the agentic coding loop for a spec-backed implementation by anchoring the plan in external artifacts so
  no progress is lost across context resets. Creates a features.json feature list and a
  codex-progress.md status file under gitignored .codex/tmp/ scratch (not committed). Use when
  starting a multi-slice docs/specs implementation, when the user says "init the loop", "set up the feature list",
  or "/loop-init", or as the setup step of the $cook command. Runs only after the idea/spec clarity gate
  returns CLEAR and an implementation plan exists.
---

# Loop Init

Set up the durable artifacts that let the coding loop survive context resets. The feature list and
progress file live **outside** the conversation, so a fresh session can pick up exactly where the
last one stopped.

## Precondition (hard gate)

Do NOT initialize until:

1. `requirement-analyst` has returned **CLEAR** for the idea/spec target.
2. A committed or selected `docs/specs/<...>.md` implementation spec exists.
3. `.codex/tmp/implementation-plan.md` exists, or the caller provides an equivalent approved plan.

On **NEEDS-CLARIFICATION** or **NEEDS-SPEC-REVISION**, stop and resolve the questions before
initializing. Inside `$cook`, these gates are run before this skill.

## Inputs

- A selected `docs/specs/<...>.md` implementation spec.
- `.codex/tmp/implementation-plan.md` or an equivalent approved plan.
- Optional user idea / feature id for traceability.

## Steps

1. **Read the spec and approved plan.** Decompose it into a flat, ordered list of small,
   independently shippable slices - each one buildable and verifiable on its own.
2. **Write `.codex/tmp/features.json`.** Use JSON (models overwrite structured JSON less often than
   Markdown prose). Each feature is a **contract**, not just a title - it declares the action space
   the loop must stay inside and the objective evidence that proves it done (PEV plan-as-contract).
   One object per feature:
   ```json
   {
     "specPath": "docs/specs/<spec>.md",
     "workItemId": "<feature id, idea slug, or N/A>",
     "title": "<spec title>",
     "createdReference": "<spec path + implementation plan reference>",
     "rollbackPoint": "<git sha at loop start - `git rev-parse HEAD`>",
     "features": [
       {
         "id": 1,
         "title": "<short imperative slice title>",
         "acceptanceCriteria": ["<criterion the acceptance-verifier will check>"],
         "filesTouched": ["src/Api/Endpoints/<...>", "web/src/<...>"],
         "invariants": ["decimal for money", "no sensitive user data in logs", "DateTimeOffset via TimeProvider"],
         "validationCommands": ["dotnet test --filter <...>", "yarn --cwd web quality"],
         "riskTier": "read-only | sandbox-edit | full-access",
         "convergenceCriteria": "all validationCommands green + acceptance-verifier PASS + coverage 100%",
         "status": "pending",
         "notes": ""
       }
     ]
   }
   ```
   Field meaning:
   - **filesTouched** - the *allowed* edit space for this feature. Work outside it is scope drift; if
     you must touch a file not listed, update the contract first, don't silently widen it.
   - **invariants** - properties that must still hold after the change (money is `decimal`, no user
     data in logs/errors per sensitive data policy, `DateTimeOffset`/`TimeProvider` for time, etc.). Draw from the
     path-scoped rules for the area being touched.
   - **validationCommands** - the deterministic sensors that prove *this* feature. `loop-next` runs
     these to verify instead of guessing "done". Prefer the narrowest command that still proves it.
   - **rollbackPoint** - top-level: the git sha at loop start, so a feature can be reset cleanly.
   - **riskTier** - which permission tier the feature needs (see `docs/harness/permission-tiers.md`).
     Anything `full-access` (network, credential, deploy, destructive delete, migration) requires HITL.
   - **convergenceCriteria** - the explicit, objective stop condition. Never *implicit*
     ("feels done").

   `status` is one of `pending`, `in-progress`, `done`. Every feature starts `pending`. Set
   `rollbackPoint` once, at init, from `git rev-parse HEAD`.
3. **Write `.codex/tmp/codex-progress.md`** - a human-readable status mirror:
   ```markdown
   # Progress - <spec title>

   - [ ] 1. <feature title>
   - [ ] 2. <feature title>

   ## Log
   - <timestamp/context note> - initialized feature list (<n> features)
   ```
4. **Do not commit these artifacts.** `.codex/tmp/**` is gitignored scratch, so `git add` refuses
   it - there is no initialization commit. Durability comes from the file *on disk*: it survives
   conversation/context resets in this working tree, and a later `loop-next` re-reads it. Keeping it
   out of git also keeps loop bookkeeping out of the feature commits and the PR.

## Output

Confirm the feature count and the two artifact paths. Then hand off to `$loop-next` (or the
`$cook` flow) to implement one feature at a time.

## Notes

- `.codex/tmp/**` is Edit-allowlisted (write without prompts) and gitignored (never committed, never
  reaches a PR). It is the loop's external scratch memory, not repository content.
- Keep slices small: if a slice cannot be built and left green in one focused session, split it.
- Do not begin implementing here - this skill only sets up the artifacts.






