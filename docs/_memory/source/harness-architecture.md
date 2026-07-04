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

This note is the repository contract for Codex-facing automation and harness boundaries.

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
| Evals | `harness/evals/` | Harness-owned eval surface for deterministic regression checks across hooks, graph, agent behavior, and future runtime behavior |
| Runtime contract | `harness/` | Machine-readable orchestration, policy, telemetry, and tool contracts; no EventHub product logic |

## Boundaries

Hooks are lifecycle interception only. Guardrails and enforcement data live in policy files.

Skills describe workflows. Scripts implement repeatable command surfaces. CLI examples such as `kubectl` or trace readers belong in skill/tool standards, not in the core harness architecture.

Memory has four lanes:

- working memory: active Codex conversation context
- task memory: `.codex/plans/`, `.codex/notes/`, generated handoff text, eval results
- long-term knowledge memory: `docs/` as an Obsidian vault, including source docs, MOCs, glossaries, and retrieval guides
- runtime state: `.codex/state/`, always rebuildable or temporary

Long-term memory is validated through `scripts/agent/Test-DocsMemory.ps1` and mapped in `.graph/index.json` for `docs/README.md`, `docs/.obsidian/`, and `docs/_memory/`.

Monitoring is evidence for improvement, not decoration. `harness/evals/results/latest.json`, hook outcomes, and command exit codes are the first observability layer.

## Workflow Harness Contract

The `cook-unified` workflow is the single entrypoint for feature delivery. Inside `cook`, the harness-owned phases are `spec` -> `plan` -> checkpoint implementation -> verify -> memory sync -> handoff.

The workflow must keep harness impact and long-term memory sync explicit:

- The spec phase records whether the feature touches evals, orchestrator, policies, telemetry, tools, or workflow surfaces, and makes the new spec discoverable from the relevant long-term memory surfaces. For feature specs this includes `docs/_memory/mocs/feature-roadmap.md`; for other durable knowledge it may include source maps, MOCs, glossaries, retrieval guides, or README/index files.
- The plan phase translates every non-`N/A` harness impact into concrete files, tasks, and validation commands, and includes a Memory Sync inventory owned by `memory-sync` for spec status, source docs, MOCs, glossaries, retrieval guides, README/index files, harness contracts, graph/routing data, and docs-memory validation.
- The implementation phase stops on missing harness-impact or Memory Sync planning, updates the plan when new impact or memory drift appears, runs harness evals for harness changes, marks the related spec implemented only after objective checks pass, refreshes every affected long-term memory and harness contract surface, and runs docs-memory plus changed-code verification before handoff.

Harness changes must not be hidden inside product implementation tasks. Changes to `harness/evals/`, `harness/orchestrator/`, `.codex/policies/`, `harness/policies/`, `harness/telemetry/`, `harness/tools/`, `.agents/skills/`, `.codex/hooks/`, `scripts/agent/`, `.graph/`, or AGENTS.md require visible plan entries and objective verification.


## Workflow Skills

`memory-sync` owns durable docs-memory completion audits for spec-backed implementation and workflow changes. Use it after `/cook` implementation checks pass and before final handoff or PR creation. It is a workflow skill, not a runtime harness lane; runtime lane skills remain listed below.
## Harness Lane Skills

Each harness lane has a dedicated repo-local skill with a standard professional format:

| Lane | Skill | Owns |
|---|---|---|
| Evals | `harness-evals` | `harness/evals/` cases, fixtures, runner evidence, manual agent cases |
| Orchestrator | `harness-orchestrator` | `harness/orchestrator/` TaskSpec routing, handoffs, retries, approvals, stop conditions |
| Policies | `harness-policies` | `.codex/policies/`, `harness/policies/`, guardrails, permission and approval mapping |
| Telemetry | `harness-telemetry` | `harness/telemetry/`, traces, logs, metrics, evidence, improvement-loop records |
| Tools | `harness-tools` | `harness/tools/`, hosted tool, MCP, and local CLI adapter contracts |

Use `scripts/agent/New-HarnessSkill.ps1` to scaffold or refresh a harness lane skill skeleton when a new lane is introduced or a lane skill needs standard metadata.

## Future Runtime

If EventHub needs an application-owned orchestrator, build it under `harness/` with:

- Responses API as the model contract
- Agents SDK for orchestration, guardrails, handoffs, and tracing
- Codex CLI exposed via MCP only when a workflow needs external multi-step coding execution

Do not start new runtime work on Assistants API.

All eval cases live under `harness/evals/` because evals are part of the harness. Do not add a root `evals/` tree; use case `layer` and `id` naming to distinguish runtime, hook, graph, and agent checks.

## Runtime Artifacts

The harness must expose real machine-readable artifacts, not placeholder README files:

| Artifact | Purpose |
|---|---|
| `harness/manifest.json` | Lane registry, status command, eval command, and artifact inventory |
| `harness/orchestrator/task-spec.schema.json` | TaskSpec and Harness Impact schema |
| `harness/orchestrator/routing.json` | `cook-unified` routing, phase contract, and lane-skill routing |
| `harness/policies/runtime-policy.json` | Runtime permission, approval, protected path, and hook enforcement contract |
| `harness/telemetry/events.schema.json` | Harness event schema and redaction contract |
| `harness/tools/registry.json` | Agent-facing command/tool registry and side-effect declarations |
| `scripts/agent/Get-HarnessStatus.ps1` | Status command that validates these artifacts and fails on placeholder README scaffolds |

Do not add `README.md` files under `harness/`, `harness/orchestrator/`, `harness/policies/`, `harness/telemetry/`, or `harness/tools/`.

## Done Criteria

The repo harness is useful when an agent can:

- bootstrap environment state with `repo-bootstrap`
- map a diff to checks with `verify-changed-code`
- hand off work with explicit files and verification evidence
- rely on hooks to block protected paths and known-dangerous commands
- run `scripts/agent/Get-HarnessStatus.ps1 -Json` and get `status: passed`
- run `harness/evals/run.ps1 -Layer harness` after hook or policy changes
