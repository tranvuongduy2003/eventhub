# Reasoning loop (Layer 3)

## ReAct (default for every task)

Alternate **reason → act → observe** — do not batch many tools without reading results.

1. **Reason** — one short intent: what you will verify or change and why.
2. **Act** — one tool (or one logical edit batch).
3. **Observe** — read exit code, linter output, test result, file content; update `.opencode/notes/progress.md` **Decisions** if the outcome matters after compaction.

Do not declare understanding until you have observed the tool result. Do not chain speculative edits across layers without a build/test signal in between.

## Reflexion (before "done")

Self-critique without external signal is unreliable. **Done** is allowed only when objective checks pass:

| Gate | What runs |
|------|-----------|
| `file.edited` (harness plugin) | lint/format + affected test/build per changed file |
| `session.idle` (`stop-gate.ps1`) | TypeScript (`tsc -b`) if web changed; affected checks on full `git diff` |
| Verify gate | Blocks `bash` / `task` until post-edit checks pass |

If the agent tries to finish while checks fail, the harness plugin throws on `session.idle` with a follow-up message — treat it as ground truth, fix, re-run checks, then continue.

After substantial implementation, invoke **`@code-reviewer`** or delegate via the **task** tool.

For `/plan`, launch parallel **readonly** workers only — see `core.md` subagent table.

## Orchestration topology (Layer 5)

> **Multi-agent for parallel read-only research; single agent for writing code** where decisions couple.

| Mode | Who | Parallel? |
|------|-----|-----------|
| Read / scout | `codebase-explorer`, `plan-*-researcher`, `graph-impact-analyst`, `test-impact-analyzer` | **Yes** — isolated windows, summaries back to parent |
| Write code | **Parent agent only** | **No** — never spawn multiple writers on the same change |
| Write tests (bug path) | `build-test-writer` then parent fix | **Sequential** — test writer first, then parent production code |
| Review | `code-reviewer` after parent finishes | Sequential |

**Golden rule:** parallel subtask calls must all target **readonly** subagents. If two subagents would make conflicting implementation choices, keep one writer (the parent).

Use project subagents in `.opencode/agents/` — prefer `@codebase-explorer` or `plan-*-researcher` over built-in `@explore` for EventHub work.

## Not default

**Tree-of-Thoughts** — only for explicit multi-branch search problems (architecture options, complex bug hypotheses). Not for routine feature work.

## Workflow tie-in

- `/build` completion: `dotnet test`, `yarn --cwd web lint`, update `progress.md` **Next** checkboxes.
- Before PR: `.\evals\run.ps1` when hooks or graph changed.
- Worked example (all layers): `agent-stack.md` — add `Phone` to `User`.
