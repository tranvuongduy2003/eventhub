# Harness Runtime Scaffold

This folder is reserved for an application-owned orchestration runtime if EventHub needs one later.

The repo harness already lives in:

- `AGENTS.md`
- `.agents/skills/`
- `.codex/hooks/`
- `.codex/policies/`
- `scripts/agent/`
- `evals/`

Runtime code added here must not contain EventHub product logic. It should orchestrate agent runs, tools, policies, state, telemetry, and evals.

Preferred future stack:

- Responses API for model interaction
- Agents SDK for orchestration, guardrails, handoffs, and tracing
- Codex CLI as MCP executor only when needed for multi-step coding workflows

