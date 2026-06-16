# Context memory (compaction-safe)

Long tasks lose detail when OpenCode **compacts** context. Use durable files instead of relying on chat history.

## Session start

1. Read `.opencode/notes/progress.md` if it exists; otherwise create it from `.opencode/notes/progress.example.md`.
2. Align work with **Goal**, **Decisions**, and **Next** before new exploration.

## During work

Update `progress.md` after milestones: task completed, architecture decision, blocker found, or before delegating to a subagent.

**Write to Decisions:** approach chosen, files created/changed, spec/feature ids, anything you would regret losing after compact.

Do not dump tool output — bullets only.

## Subagents (Task tool)

Before launching a subagent, add one line to `progress.md` **Status** (what the worker should do).

Project subagents (`core.md` table — e.g. `plan-domain-researcher`, `graph-impact-analyst`):

1. Read `.opencode/agent-memory/<agent-name>.md` at start if present.
2. After non-trivial work, append **durable** codepaths/patterns only (under ~150 lines; no secrets).

**Worker memory format:** `# <agent-name>` · sections `Codepaths`, `Patterns`, `Gotchas` — bullets only.

Parent owns `progress.md`; each worker owns `agent-memory/<agent-name>.md`.

## After compaction

Re-read `progress.md` and latest `.opencode/notes/backups/<timestamp>/` (transcript + notes from `preCompact` hook in `harness.md`).
