---
title: EventHub Agent Harness Architecture
type: source
status: active
tags:
  - harness
  - source
  - architecture
---

# EventHub Agent Harness Architecture

This note turns `deep-research-report.md` into the repository contract for Codex-facing automation.

## Definition

The harness is the contract around an agent run:

- repository guidance
- skills and execution scripts
- tool and filesystem policy
- lifecycle hooks
- task state and memory artifacts
- verification and evals
- future orchestration runtime

It is not a random collection of prompt notes, CLI examples, or product implementation code.

## Current Layers

| Layer | Location | Role |
|---|---|---|
| Repo guidance | `AGENTS.md` | Short working agreement, source-of-truth routing, non-negotiable rules |
| Policy | `.codex/policies/harness-policy.json` | Protected paths, blocked shell commands, verify-gate behavior |
| Hooks | `.codex/hooks/` | Lifecycle interception: pre-tool, pre-shell, post-edit, stop |
| Skills | `.agents/skills/` | Reusable workflows loaded only when relevant |
| Execution scripts | `scripts/agent/` and `scripts/affected-tests.mjs` | Stable, agent-friendly command surface |
| Verification graph | `.graph/index.json` | Path-to-check mapping for changed files |
| State | `.codex/state/` | Runtime artifacts such as verify gate state; gitignored |
| Evals | `evals/` | Deterministic regression checks for hooks, graph, and agent behavior |
| Runtime scaffold | `harness/` | Future orchestration boundary; no EventHub product logic |

## Boundaries

Hooks are lifecycle interception only. Guardrails and enforcement data live in policy files.

Skills describe workflows. Scripts implement repeatable command surfaces. CLI examples such as `kubectl` or trace readers belong in skill/tool standards, not in the core harness architecture.

Memory has four lanes:

- working memory: active Codex conversation context
- task memory: `.codex/plans/`, `.codex/notes/`, generated handoff text, eval results
- long-term knowledge memory: `docs/` as an Obsidian vault, including source docs, MOCs, glossaries, and retrieval guides
- runtime state: `.codex/state/`, always rebuildable or temporary

Long-term memory is validated through `scripts/agent/Test-DocsMemory.ps1` and mapped in `.graph/index.json` for `docs/README.md`, `docs/.obsidian/`, and `docs/_memory/`.

Monitoring is evidence for improvement, not decoration. `evals/results/latest.json`, hook outcomes, and command exit codes are the first observability layer.

## Future Runtime

If EventHub needs an application-owned orchestrator, build it under `harness/` with:

- Responses API as the model contract
- Agents SDK for orchestration, guardrails, handoffs, and tracing
- Codex CLI exposed via MCP only when a workflow needs external multi-step coding execution

Do not start new runtime work on Assistants API.

## Done Criteria

The repo harness is useful when an agent can:

- bootstrap environment state with `repo-bootstrap`
- map a diff to checks with `verify-changed-code`
- hand off work with explicit files and verification evidence
- rely on hooks to block protected paths and known-dangerous commands
- run `evals/run.ps1 -Layer harness` after hook or policy changes
