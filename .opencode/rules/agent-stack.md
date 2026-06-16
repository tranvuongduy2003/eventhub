# Five-layer walkthrough — add `Phone` to `User`

Ad-hoc ask: *"Thêm field `phone` vào model `User`."*  
EventHub path: **`/spec` → `/plan` → `/build`** for tracked features; small scoped changes still follow **all five layers** (use `/build` with a one-task plan or explicit user request).

**No layer is optional.** Skip context → blind edits miss callers. Skip harness → agent "feels done" while tests red. Skip topology → parallel writers contradict. Skip notes → compaction loses decisions.

---

## Layer 1 — Context + notes

**Before editing**, parent spawns **parallel readonly** subagents (Layer 5):

```text
@graph-impact-analyst — blast radius: add Phone to User aggregate F-* …
@codebase-explorer — User aggregate, persistence mapper, API users endpoints, tests
```

- **neo4j-graphrag** MCP: callers, dependents, contract touch (e.g. Users BC → `src/Api/`, OpenAPI).
- Workers return **summaries** — path:line tables, not 12 full files in the main window.

Parent records in `.opencode/notes/progress.md` **Goal** / **Decisions**:

```markdown
## Goal
Add Phone to User (value object or string per ddd.md)

## Decisions
- graph: ~N touch points — Domain, Infrastructure mapper, Api, OpenAPI, integration tests
```

---

## Layer 4 — Workflow (`/build` task loop)

Orchestrator (**parent only**) implements after plan task lists concrete paths, e.g.:

| Layer | Files (example) |
|-------|-----------------|
| Domain | `src/Domain/Users/User.cs`, maybe `PhoneNumber.cs` value object |
| Application | commands/DTOs if registration changes |
| Infrastructure | `UserRecord`, `UserConfiguration`, `UserPersistenceMapper`, **migration** |
| Api + contract | `src/Api/Endpoints/UsersEndpoint.cs`, `contracts/openapi/api.v1.yaml` → `yarn api:codegen` |
| Tests | `tests/Domain.UnitTests/Users/…`, `tests/Api.IntegrationTests/Users/…` |

---

## Layer 3 — Reasoning (ReAct + Reflexion)

1. **ReAct:** reason → act → observe each step (no batch edits across layers without build signal).
2. **Red test first** (behavior change): delegate `@build-test-writer` — e.g. API/register response includes `phone` or domain invariant test; confirm **red once**.
3. **Single writer:** parent applies minimal diff — `User`, mapper, migration, OpenAPI, endpoint mapping.
4. Agent **cannot** self-declare done — only green checks count (`reasoning-loop.md`).

---

## Layer 2 — Harness (deterministic)

On each **write/edit**, harness `file.edited` → `post-edit-verify.ps1`:

1. `node scripts/affected-tests.mjs src/Domain/Users/User.cs` → e.g. `dotnet format` + filtered `Domain.UnitTests` + `dotnet build` Application.
2. Edit OpenAPI / Api without updating contract → eslint/tsc or **`api:verify`** / integration test **red**.
3. **Verify gate** set → harness blocks `bash` / `task` until fix saved and reverified.

Example failure: forgot OpenAPI `phone` field → integration or `yarn --cwd web api:verify` fails → gate active → agent must fix schema, not say "done".

**Stop gate** (`stop-gate.ps1` on `session.idle`): full `git diff` + `tsc` (if web) + deduped affected steps must pass — else harness throws and the build session continues.

---

## Layer 5 — Topology

| Phase | Agents | Parallel? |
|-------|--------|-----------|
| Scout | `@graph-impact-analyst`, `@codebase-explorer`, `@test-impact-analyzer` | Yes (`permission.edit: deny`) |
| Red test | `@build-test-writer` | No — alone |
| Production code | **build** primary agent | No — never parallel writers |
| Review | `@code-reviewer` | After parent green |

---

## Layer 1 again — Note-taking (survives compact)

When checks green, update `progress.md`:

```markdown
## Status
Phone on User complete — migration applied, OpenAPI synced

## Decisions
- Phone as value object `PhoneNumber` in Domain
- Touched: User.cs, UserPersistenceMapper, UsersEndpoint, api.v1.yaml

## Next
- [ ] Manual smoke register/login if needed
```

**PreCompact** hook backs up `progress.md` + transcript to `.opencode/notes/backups/`.

---

## Checklist (same request, ordered)

| Step | Layer | What happens |
|------|-------|----------------|
| 1 | 1 + 5 | Parallel readonly graph + scout → summary in main window |
| 2 | 3 | Red test via `build-test-writer` |
| 3 | 4 + 5 | Parent edits Domain → Infra → Api → contract (single writer) |
| 4 | 2 | Each save → `post-edit-verify`; gate blocks if red |
| 5 | 3 | Fix until tests/type green (no self-grade) |
| 6 | 2 | Stop gate runs full diff checks |
| 7 | 1 | `progress.md` updated for next session / compact |

Regression after harness changes: `.\evals\run.ps1`.
