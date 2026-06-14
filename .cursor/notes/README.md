# Structured notes (context-rot mitigation)

Durable scratchpad outside the chat context window. Agents read and update these files so work survives **compaction** and **new sessions**.

## Files

| Path | Purpose |
|------|---------|
| `progress.md` | Main session scratchpad — where we are, decisions, remaining work |
| `progress.example.md` | Committed template; copy to `progress.md` on first use |
| `backups/` | Auto snapshots from the `preCompact` hook (transcript + notes) |

## `progress.md` sections

Keep each section short (bullets, not prose). Update after meaningful milestones — not every tool call.

1. **Goal** — one line: feature id, spec path, or user ask
2. **Status** — current task / last completed step
3. **Decisions** — architecture or product choices (`DEC-*`, approach picked, files touched)
4. **Blockers / open questions**
5. **Next** — 3–5 concrete next actions

## PreCompact backups

When Cursor compacts context (auto or manual), `.cursor/hooks/pre-compact-backup.ps1` runs and saves under `backups/<timestamp>-<conv>/`:

- `precompact-meta.json` — token usage, trigger, conversation id
- `transcript.jsonl` — full transcript (if transcripts enabled in Cursor settings)
- `progress.md` — copy at compact time
- `agent-memory/` — snapshot of worker memory files

Last **20** backup folders are kept; older ones are pruned.

## Related

- Worker-layer memory: `.cursor/agent-memory/<agent-name>.md` (one file per project subagent)
- Agent rule: [`context-memory.mdc`](../rules/context-memory.mdc) · hooks: [`harness.mdc`](../rules/harness.mdc)
