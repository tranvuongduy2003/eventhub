# EVENTHUB - Codex project instructions

EventHub is a Clean Architecture + CQRS + DDD event management and ticketing platform. Local topology is **.NET Aspire only**.

Backend topology:

- `.NET` Minimal API composition root in `Api`
- Clean Architecture layers: `Domain`, `Application`, `Infrastructure`, `Contracts`
- MediatR command/query flows in `Application`
- EF Core persistence in `Infrastructure`
- `ServiceDefaults` shared by local services
- PostgreSQL as authoritative persistence
- Redis as rebuildable cache
- MinIO for binary assets
- `System.Threading.Channels` for in-process integration events across bounded contexts

Frontend topology:

- React + TypeScript + Vite
- Yarn
- Tailwind/shadcn where applicable
- Generated OpenAPI client/types consumed from the contract pipeline

This is the root `AGENTS.md`. Product, feature, and technical truth lives in the three specifications listed below. Procedural conventions live in repo-local skills, custom agents, hooks, policies, and nested `AGENTS.md` files.

Codex loads `AGENTS.md` files from the project root down to the session working directory. A nested file is therefore automatic only when the session starts in that subtree. For sessions started at the repository root, read the matching nested file from the routing table before editing that area. Nested instructions refine local implementation; they do not override `product.md`, `features.md`, or `technical.md`.

## Source of truth

Read the smallest relevant set before acting. These are the only durable EventHub product/feature/technical specifications:

| Document            | Authority                                                                                                                   |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `docs/product.md`   | Product intent, personas, scope, non-goals, decisions (`DEC-*`), guardrails (`QG-*`)                                        |
| `docs/features.md`  | Epics (`EP-*`), features (`F-*`), dependencies, acceptance criteria, delivery status                                        |
| `docs/technical.md` | Architecture (`ARCH-*`), bounded contexts, aggregates, invariants (`INV-*`), data, integrations, security, and verification |

Harness documents, skills, agents, hooks, and policies may define workflow mechanics, but they MUST NOT become competing sources of product behavior or architecture.

Precedence for repository decisions:

1. `docs/product.md` defines allowed outcomes and scope.
2. `docs/features.md` defines observable behavior within that scope.
3. `docs/technical.md` defines a conforming implementation.
4. This root `AGENTS.md` defines repository-wide working constraints.
5. Nested `AGENTS.md` files refine instructions for their subtree.
6. Skills define repeatable procedures.
7. Local code discovery supplies implementation context.

When lower-level instructions or code drift from a higher-level source, follow the higher-level source and report the drift. Do not silently preserve stale behavior or invent new product behavior.

## Non-negotiables

1. Architecture flows `Domain -> Application -> Infrastructure`; `Api` is the composition root.
2. `EventHub.Domain` is pure C#: no EF Core, ASP.NET, MediatR, Infrastructure references, persistence annotations, or transport concerns.
3. Commands and queries stay separated in `Application` and flow through MediatR.
4. API endpoints are thin. They map HTTP to application use cases and return `Contracts` DTOs, never domain entities.
5. OpenAPI shape lives in `contracts/openapi/api.v1.yaml`; do not hand-edit generated web API output.
6. Aspire AppHost is the local topology source of truth. Do not add hand-authored `docker-compose.yml`.
7. `EventHub.ServiceDefaults` is mandatory for `Api`.
8. PostgreSQL is authoritative. Redis is rebuildable cache only. MinIO stores binary assets.
9. Domain model follows `technical.md`: modular-monolith boundaries, aggregate invariants, typed identities, and idempotent Channel-based integration events across bounded contexts.
10. Tests must be meaningful and selective: Domain unit tests, Api integration tests, and Playwright e2e only when behavior crosses the UI boundary or needs browser verification.
11. Frontend route guards are UX support only. Protected operations must be enforced on the backend.
12. Do not add production dependencies without explicit approval and a license check.

## Repository layout

```text
src/       AppHost, ServiceDefaults, Api, Application, Domain, Infrastructure, Contracts, DataSeeder
tests/     Domain.UnitTests, Api.IntegrationTests, Testing.Common
e2e/       Playwright e2e tests
web/       React + Vite frontend; Yarn; run through Aspire web resource
docs/      product, feature, technical, implementation specs, and harness docs
contracts/ OpenAPI contract and codegen scripts
.codex/    Codex config, hooks, local state, custom agents
.agents/   Repo-local skills and workflow procedures
scripts/   Stable agent and verification scripts
```

Do not infer additional runtime, graph, state, or verification directories from older instructions. Active workflow surfaces must be discovered from the current repository and source documents.

## Harness operating model - Plan -> Execute -> Verify

The harness runs as a cybernetic governor: it observes the effect of every action through deterministic sensors such as hooks, formatters, type checks, tests, static analysis, and acceptance verification. Every non-trivial task follows **Plan -> Execute -> Verify**:

1. The plan is a contract: intended files/areas, invariants, validation commands, rollback point, risk tier, and approval boundary.
2. Execution uses the lowest sufficient permission tier: read-only, workspace edit, or explicit human-confirmed full access.
3. Termination is decided by verification, not model confidence: a feature converges only on green sensors and acceptance-verifier PASS.
4. Workflow changes are validated through their owning skill, hook, policy, validator, or verification script before promotion.

Reference workflow mechanics from the active harness documents discovered in the repository, plus `.agents/skills/`, `.codex/agents/`, `.codex/hooks.json`, `.codex/hooks/`, and `.codex/policies/`. Product behavior and architecture remain governed only by the three specifications.

## Invoke the matching skill FIRST

Skills hold current how-to, templates, references, and scripts. Invoke the relevant one(s) at the start of a task. Do not load broad or overlapping skills when one narrower skill owns the work.

| Trigger                                                                                                            | Skill                                    |
| ------------------------------------------------------------------------------------------------------------------ | ---------------------------------------- |
| Full idea/spec/task loop                                                                                           | `$cook`                                  |
| Backend: aggregate, value object, DbContext, migration, EF query, endpoint, ProblemDetails, backend test, coverage | `$backend`                               |
| Frontend: component, hook, module, data fetching, form, styling, route guard                                       | `$frontend`                              |
| E2E/API tests: Playwright, page object, API client, test plan, step/verifyPoint                                    | `$e2e`                                   |
| shadcn UI primitives / `components.json`                                                                           | `$shadcn`                                |
| Zustand stores                                                                                                     | `$zustand-web`                           |
| Browser automation / verify UI / console / network / performance                                                   | `$chrome-devtools`                       |
| Aspire AppHost inspection                                                                                          | `$aspire-mcp`                            |
| Library/framework/API docs                                                                                         | `$context7-research`                     |
| Create/update a skill                                                                                              | `$skill-creator`                         |
| Agentic loop state                                                                                                 | `$loop-init` -> `$loop-next`             |
| Open a PR handoff                                                                                                  | `$create-pr`                             |
| Harness review                                                                                                     | `$harness-review` using `harness-doctor` |

If a skill name exists only as a planned migration target and is not present in the repo yet, stop and report the missing skill instead of substituting an unrelated workflow.

## Nested AGENTS routing

Before editing a scoped area, read the nearest matching `AGENTS.md` even when the current Codex session was started at the repository root.

| Instructions                   | Applies to                                                                             |
| ------------------------------ | -------------------------------------------------------------------------------------- |
| `src/AGENTS.md`                | All backend and host code under `src/**`                                               |
| `src/Domain/AGENTS.md`         | Aggregates, entities, value objects, domain services, domain events                    |
| `src/Application/AGENTS.md`    | Commands, queries, handlers, validators, behaviors, ports, authorization orchestration |
| `src/Infrastructure/AGENTS.md` | EF Core, repositories, migrations, cache, storage, messaging, payment/email adapters   |
| `src/Api/AGENTS.md`            | Endpoints, HTTP mapping, auth/session transport, ProblemDetails, webhooks, SignalR     |
| `src/Contracts/AGENTS.md`      | Explicit request/response contract types                                               |
| `contracts/AGENTS.md`          | Committed OpenAPI source and contract-generation workflow                              |
| `tests/AGENTS.md`              | Domain unit, application/component, API/consumer integration tests and shared fixtures |
| `web/AGENTS.md`                | React/Vite frontend, forms, server/client state, accessibility, route guards           |
| `web/src/generated/AGENTS.md`  | Generated API output; source-only changes are forbidden here                           |
| `e2e/AGENTS.md`                | Playwright journeys, page objects, fixtures, browser-level verification                |

When a change spans multiple scopes, read every applicable file. The most specific file governs local implementation details; cross-scope behavior still has to satisfy all applicable specifications and verification requirements.

## Custom agents

Project-scoped agents live under `.codex/agents/`.

| Agent                    | Use                                                                                                | Writes?           |
| ------------------------ | -------------------------------------------------------------------------------------------------- | ----------------- |
| `requirement-analyst`    | First read-only idea/spec clarity gate; returns `CLEAR` or `NEEDS-CLARIFICATION`                   | no                |
| `spec-brainstormer`      | Strong-reasoning product/spec synthesis into `docs/specs/`                                         | specs only        |
| `implementation-planner` | Strong-reasoning plan and slice contract generation under `.codex/tmp/`                            | scratch plan only |
| `implementer`            | Medium-reasoning implementation of one scoped spec slice                                           | yes               |
| `test-writer`            | Writes focused tests                                                                               | tests only        |
| `code-reviewer`          | Path-aware, evidence-based read-only review                                                        | no                |
| `security-reviewer`      | Auth/session/sensitive-data/injection/dependency review                                            | no                |
| `acceptance-verifier`    | Final read-only gate; diffs implementation against spec acceptance criteria                        | no                |
| `harness-doctor`         | Diagnoses harness behavior from telemetry, hooks, skills, agents, scripts, and validation evidence | no                |

Reviewers, gates, and `harness-doctor` are read-only. Use subagents for parallel read-heavy work such as exploration, test-gap analysis, security review, and acceptance verification. Coordinate write-heavy work in the main thread unless files are explicitly independent.

## Commands as skills

- **`$cook <idea-or-feature>`**: turn an idea or `docs/features.md` target into a full `docs/specs/` implementation spec, plan it, implement slices, review, verify, and optionally prepare a PR handoff.
- **`$harness-review`**: diagnose harness behavior from telemetry and propose governed changes.
- **Local dev**: `$aspire-mcp`.

## Workflow

Use ReAct: verify context, act, observe the result. Do not declare done without objective checks.

For implementation:

1. Read the relevant specification(s) and every applicable nested `AGENTS.md`.
2. Invoke the matching skill first.
3. Use `requirement-analyst` when the source requirement is unclear.
4. Plan intended files/areas, invariants, validation commands, rollback point, risk tier, and approval boundary.
5. Scout code paths before editing.
6. For bugs, write or update a red test first when feasible.
7. Edit narrowly.
8. Run the affected checks.
9. For substantial work, run `code-reviewer`, `security-reviewer` when relevant, and `acceptance-verifier`.
10. Handoff with changed files, checks run, acceptance status, and residual risk.

Do not add low-value tests that assert implementation details or the obvious.

## Agentic loop state

Use `$loop-init` before a multi-step agentic loop and `$loop-next` between iterations. The loop state must track:

- active requirement or acceptance criterion;
- current hypothesis;
- last action;
- observed result;
- next action;
- blockers;
- verification state.

Do not hard-code loop state storage paths in this root file. The owning skill or workflow defines where state is stored, how it is serialized, and whether it is persistent. Record only decisions, changed files, blockers, and next steps. Do not dump command output, secrets, tokens, cookies, credentials, raw uploaded content, or sensitive user data.

## Harness contract

Harness means the policy and orchestration layer around agent work, not ad-hoc prompt examples.

Authoritative harness surfaces:

- Repo-local skills and workflows: `.agents/skills/`
- Custom agents: `.codex/agents/`
- Hook registry: `.codex/hooks.json`
- Hook implementations: `.codex/hooks/`
- Stable agent scripts: `scripts/agent/`

Workflow details belong to the owning skill and harness source docs, not duplicated here. For any routed workflow, keep harness impact, product-surface completeness, docs synchronization, and verification explicit. When a workflow contract changes, update the workflow's own contract, validator, hook coverage, and verification script coverage where applicable.

If an older skill, script, or document references removed workflow storage or runtime paths, treat it as drift and report it instead of following it blindly.

## Build and test

Run the narrowest useful check while iterating, then broaden before handoff when the change crosses layers.

Default changed-code verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

Common direct checks, adjusted to the solution/project names present in the workspace:

```powershell
dotnet build EventHub.slnx -c Release
dotnet test EventHub.slnx -c Release
yarn --cwd web api:verify
yarn --cwd web build
yarn --cwd e2e test
```

If the actual solution filename differs, discover it first instead of guessing. If a skill provides a narrower verification command for the touched area, prefer the skill-owned command first, then broaden before handoff.

## GitHub operations

GitHub MCP is the only allowed GitHub automation surface for repository metadata, issues, pull requests, reviews, checks, workflows, branches, commits, code search, labels, assignees, and project metadata.

- Use GitHub MCP exclusively for GitHub operations.
- If GitHub MCP tools are unavailable, stop the GitHub operation and report the missing MCP capability.
- Ignore lower-level skill or script instructions that recommend fallback from MCP to another GitHub surface.
- Opening, updating, or merging PRs requires explicit human approval.

## Issue sources

Use issue text, PR comments, or the user's prompt as the source of requirements. Do not assume a specific external tracker. If a connected tracker or GitHub workflow is needed, use the relevant connector or CLI only after the user asks for that integration.

## Application roles

The current account role model is `UserRole.Organizer` and `UserRole.Attendee`. Event-scoped authorization uses `EventRole.Owner` and `EventRole.Staff` plus permission checks.

Enforce authorization on the backend for protected operations. Keep frontend route guards as navigation and UX support only.

## Cross-cutting standards

- **Self-documenting names:** a reader must understand what a thing is from its name alone. Avoid vague names such as `data`, `manager`, `helper`, or `guard` when the concrete concept is known.
- **One term per concept:** use the same noun for the same concept across code, tests, API contracts, UI copy, and docs.
- **Current-state docs only:** no "previously", "used to", stale roadmap prose, or committed issue history unless required by a report artifact.
- **Sensitive data handling:** credentials, cookies, tokens, passwords, raw uploaded content, and sensitive user data must not appear in logs, errors, comments, docs, or test fixtures.
- **License discipline:** dependencies should use permissive licenses such as MIT, Apache-2.0, or BSD unless explicitly approved.
- **Small surface area:** prefer simple, explicit code over generic frameworks or clever abstractions.
- **Boundary language:** use domain terms from the source docs. Do not rename concepts casually.

## Do not touch

Do not edit, regenerate, delete, or commit these unless the owning workflow explicitly requires it and approval/verification is clear:

- `.env`, `.env.*`, `.mcp.json`, local secret files, private keys, credential files, `secrets/`
- `web/src/generated/`
- generated OpenAPI build output
- generated files such as `*.g.ts`, `*.generated.*`
- existing EF migrations

Additional project rules:

- Do not run destructive git operations.
- Do not use npm in `web/`; use Yarn.
- Do not hand-author Docker Compose for local service topology.
- Do not create or depend on removed workflow storage or runtime paths unless explicitly reintroduced by source-memory docs.
- Do not log or document secrets, cookies, tokens, passwords, raw uploaded content, or sensitive user data.

## Needs human approval

Ask for explicit approval before:

- opening, updating, or merging PRs;
- force-push, history rewrite, reset, rebase, or destructive git operations;
- production-impacting operations;
- destructive filesystem operations outside the verified target;
- secret, credential, token, or environment-file changes;
- adding production dependencies;
- changing authentication, authorization, session, or sensitive-data handling behavior;
- overriding a denied guard/hook/policy result.

If a hook or policy denies an action, stop and report the denial rather than working around it.
