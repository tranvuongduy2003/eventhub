# Harness Review - 2026-07-11

Scope: `AGENTS.md`, nested `AGENTS.md`, `.codex/**`, `.agents/**`, `docs/**`, and `scripts/agent/**`.

This review diagnoses the Codex harness, not EventHub product behavior. No harness mutation was applied.

## Evidence Read

- Root and nested instructions: `AGENTS.md`, `src/**/AGENTS.md`, `contracts/AGENTS.md`, `tests/AGENTS.md`, `web/AGENTS.md`, `e2e/AGENTS.md`.
- Harness configuration: `.codex/config.toml`, `.codex/hooks.json`, `.codex/hooks/*.ps1`, `.codex/agents/*.toml`.
- Skills: `.agents/skills/**/SKILL.md` and selected references where directly relevant.
- Docs: `docs/product.md`, `docs/features.md`, `docs/technical.md`, `docs/harness/*.md`, `docs/specs/README.md`.
- Sensors: `scripts/agent/Test-HarnessPolicy.ps1`, `scripts/agent/Test-DocsMemory.ps1`, `scripts/agent/Verify-ChangedCode.ps1`.
- Telemetry: `.codex/tmp/telemetry/*.jsonl`.

## Validation Run

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1 -Json` - passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1 -Json` - passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1 -PlanOnly -Json` - planned docs/harness sensors plus backend/web checks for the current dirty tree.
- Synthetic guard payloads for `Get-Content .env.local`, `type .env.local`, and `cat .env.local` returned no deny output, confirming the secret-read gap without reading any secret file.

## Telemetry Summary

- Telemetry files: 2.
- Records observed before the final checks: 48.
- Sessions: 2 (`policy-test`, `manual-verify`).
- Tool-call records: 0.
- Permission-deny records: 35 before final checks, 38 in the harness-doctor pass.
- Stop verification records: 1, with `changedFiles=319`, `heavySensors=false`, `problems=0`.

Telemetry is too sparse to measure real agent trajectory efficiency, recovery, or state consistency. It mostly proves that synthetic deny checks ran.

## Harness-Level Metrics

| Dimension | Assessment |
|---|---|
| Trajectory efficiency | Not measurable from current telemetry because no tool-call records exist. |
| Verification strength | Mixed. Direct docs and harness policy sensors pass, but stop verification can pass huge dirty surfaces with heavy sensors off. |
| Recovery | Not measurable. No real failing command or retry trajectory is recorded. |
| State consistency | Partially strong in design: loop artifacts live in `.codex/tmp`, and `loop-next` says trust git over progress notes. Weak in evidence because telemetry lacks read/edit traces. |
| Safety compliance | Good intent and some effective denies, but credential-read and patch-path gaps remain. |
| Replayability | Low. Current telemetry cannot reconstruct actual tool usage, patch edits, commands, or subagent work. |

## Findings

### High - Secret files can be read through shell commands

`AGENTS.md` forbids touching `.env`, secret files, private keys, and credentials. The guard blocks direct write payloads to `.env.local`, but the shell regex only catches `.env` followed by whitespace, end, or redirection. Synthetic payloads for `Get-Content .env.local`, `type .env.local`, and `cat .env.local` were allowed.

Evidence:
- `AGENTS.md:260-276`
- `.codex/hooks/guard-dangerous.ps1:84-96`
- `scripts/agent/Test-HarnessPolicy.ps1:215-223`

Impact: a model or subagent could read `.env.local` without a deny event, violating the credential boundary.

### High - Stop verification can pass a very large dirty surface without heavy checks

`verify-on-stop.ps1` runs only debug-statement scanning by default. Heavy build/typecheck is opt-in through `CODEX_STOP_VERIFY_BUILD=1`. Current telemetry has one stop verify record with `changedFiles=319`, `heavySensors=false`, `problems=0`.

Evidence:
- `.codex/hooks/verify-on-stop.ps1:1-12`
- `.codex/hooks/verify-on-stop.ps1:82-104`
- `.codex/tmp/telemetry/manual-verify.jsonl`

Impact: the harness can produce a clean stop signal for broad harness/product changes that have not run semantic, build, docs, or policy checks.

### High - Runtime hook matchers and hook scripts are coupled to older tool names

`.codex/hooks.json` matches `Bash`, `Write`, `Edit`, `MultiEdit`, and `apply_patch`, while this current Codex runtime exposes `exec_command` and `apply_patch`. The scripts also parse payloads using `tool_name`, `tool_input.file_path`, and command shapes for `Bash`. If the runtime emits different names or payloads, guard, formatting, and telemetry silently miss actions.

Evidence:
- `.codex/hooks.json:16-52`
- `.codex/hooks/guard-dangerous.ps1:58-85`
- `.codex/hooks/format-on-write.ps1:12`
- `.codex/hooks/telemetry-log.ps1:27-66`

Impact: sensors can be green in synthetic tests while not observing actual runtime tools.

### Medium - `apply_patch` is registered but not actually understood

The hook registry includes `apply_patch`, but guard and formatter logic expect a single `file_path`. A patch can touch multiple files, including protected paths, without the scripts parsing patch headers. Telemetry also records only old edit tool shapes.

Evidence:
- `.codex/hooks.json:18,31,42`
- `.codex/hooks/guard-dangerous.ps1:63-81`
- `.codex/hooks/format-on-write.ps1:12-14`
- `.codex/hooks/telemetry-log.ps1:38-66`

Impact: the preferred editing tool can bypass protected-path detection, post-write formatting, and edit-area telemetry unless the host runtime supplies a normalized path payload.

### Medium - Changed-code verification misses important harness surfaces

`Verify-ChangedCode.ps1` treats root `AGENTS.md`, `docs/`, `.codex/agents/`, `.codex/hooks/`, `.codex/hooks.json`, `.agents/skills/`, and `scripts/agent/` as docs/harness changes. It does not include `.codex/config.toml` or nested `AGENTS.md` files such as `src/AGENTS.md`, `web/AGENTS.md`, `tests/AGENTS.md`, `e2e/AGENTS.md`, and `contracts/AGENTS.md`.

Evidence:
- `scripts/agent/Verify-ChangedCode.ps1:99-108`
- `scripts/agent/Verify-ChangedCode.ps1:133-136`
- `AGENTS.md:116-134`
- `AGENTS.md:197-205`

Impact: permission/MCP config edits and scoped instruction edits can skip harness policy validation.

### Medium - GitHub MCP guidance conflicts with active config

Root `AGENTS.md` says GitHub MCP is the only allowed GitHub automation surface and PR opening/updating requires explicit approval. The `github-mcp` skill describes remote MCP configured in `.mcp.json` and says not to put GitHub tokens there. Active `.codex/config.toml` instead configures a Docker GitHub MCP using `GITHUB_PERSONAL_ACCESS_TOKEN`, and sets several GitHub write tools to `approval_mode = "approve"`.

Evidence:
- `AGENTS.md:231-238`
- `.agents/skills/github-mcp/SKILL.md:8-25`
- `.codex/config.toml:19-43`

Impact: agents can follow stale setup guidance or misunderstand where credential and human-approval boundaries actually live.

### Medium - Some agent sandbox declarations are weaker than their role language

`implementation-planner` is described as read-only in its description, but has `sandbox_mode = "workspace-write"` and may write `.codex/tmp/implementation-plan.md`. The actual allowed write is reasonable, but the role label and permission mechanism disagree.

Evidence:
- `.codex/agents/implementation-planner.toml:1-11`
- `AGENTS.md:140-152`

Impact: this is small by itself, but in a harness it blurs the difference between prompt discipline and mechanical permission boundaries.

### Low - Harness-review report path has a typo

The harness-review skill tells the agent to write `.codex/tmp$harness-review-<YYYY-MM-DD>.md`, missing a slash.

Evidence:
- `.agents/skills/harness-review/SKILL.md:34-38`

Impact: agents may create a malformed scratch path or hesitate over where to store the report.

## Governed Proposals

| # | Target | Change | Validation | HITL |
|---|---|---|---|---|
| 1 | `.codex/hooks/guard-dangerous.ps1`, `scripts/agent/Test-HarnessPolicy.ps1` | Block shell reads/writes for `.env`, `.env.*`, `secrets/`, private keys, and common read aliases. Add synthetic deny tests for `Get-Content .env.local`, `type .env.local`, `cat .env.local`. | `Test-HarnessPolicy.ps1`; targeted synthetic hook payload tests. | Yes, credential boundary. |
| 2 | `.codex/hooks.json`, `.codex/hooks/*.ps1`, `Test-HarnessPolicy.ps1` | Align hook matchers and payload parsing with the actual Codex runtime tool names and payloads; keep compatibility tests for old and new shapes only if both are truly emitted. | Synthetic payload tests for `exec_command`, `apply_patch`, write/edit shapes; `Test-HarnessPolicy.ps1`. | Yes if permission behavior changes. |
| 3 | `.codex/hooks/guard-dangerous.ps1`, `.codex/hooks/format-on-write.ps1`, `.codex/hooks/telemetry-log.ps1` | Properly parse `apply_patch` headers or remove `apply_patch` from unsupported hook matchers and rely on a supported edit event. | Patch payload tests for protected paths, multi-file edits, and area telemetry. | Yes if guard behavior changes. |
| 4 | `scripts/agent/Verify-ChangedCode.ps1` | Include `.codex/config.toml` and all `**/AGENTS.md` files in docs/harness validation routing. | `Verify-ChangedCode.ps1 -Path .codex/config.toml -PlanOnly -Json`; `Verify-ChangedCode.ps1 -Path web/AGENTS.md -PlanOnly -Json`. | No. |
| 5 | `.codex/hooks/verify-on-stop.ps1` | Add escalation for large changed surfaces and harness/security-sensitive edits when heavy sensors are off. At minimum write a distinct warning telemetry field; preferably block stop for high-risk paths unless explicit override is present. | Hook tests plus a manual stop payload with large changed count; `Test-HarnessPolicy.ps1`. | Yes, verification boundary. |
| 6 | `.agents/skills/github-mcp/SKILL.md`, `.codex/config.toml`, possibly root `AGENTS.md` | Reconcile GitHub MCP source of truth: remote vs Docker, `.mcp.json` vs `.codex/config.toml`, token model, and approval semantics for write tools. | `Test-DocsMemory.ps1`, `Test-HarnessPolicy.ps1`, manual MCP availability check. | Yes if credential/network config changes; no for docs-only clarification. |
| 7 | `.agents/skills/harness-review/SKILL.md` | Fix report path to `.codex/tmp/harness-review-<YYYY-MM-DD>.md`. | `Test-DocsMemory.ps1`, `Test-HarnessPolicy.ps1`. | No. |

## Recommended Order

1. Fix credential-read blocking first.
2. Fix runtime hook matcher/payload compatibility and `apply_patch` handling together.
3. Extend `Verify-ChangedCode.ps1` routing for `.codex/config.toml` and nested `AGENTS.md`.
4. Reconcile GitHub MCP docs/config.
5. Add stop-verification escalation once the tool telemetry is trustworthy enough to calibrate thresholds.

## Residual Risk

The current working tree is very dirty, and telemetry has no real tool-call records. That means this review is stronger on static harness defects than on measured agent behavior. Re-run this review after a few real `$cook` / `$loop-next` sessions once telemetry contains tool, command, edit-area, subagent, and verify records.
