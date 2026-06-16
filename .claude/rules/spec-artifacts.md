---
description: "Rules for durable product specs (docs/specs/) and ephemeral engineering plans (.claude/plans/). Use when running /spec, /plan, or /build — covers spec frontmatter format, plan file structure, filename conventions, and git commit rules (never commit plans)."
paths:
  - "docs/specs/**"
  - ".claude/plans/**"
  - ".claude/commands/spec.md"
  - ".claude/commands/plan.md"
  - ".claude/commands/build.md"
---

# SPEC & PLAN ARTIFACTS

Consult `CLAUDE.md` first.

**Workflow:** `/spec` → `docs/specs/` (+ one GitHub issue) · `/plan` → `.claude/plans/` (local, gitignored) · `/build` → execute plan · delete plan when done.

| Artifact | Location | Committed? |
|----------|----------|------------|
| Spec | `docs/specs/` | Yes |
| Plan | `.claude/plans/` | **No — never commit** |

There is **no** `docs/plans/` folder.

## Spec filename

`docs/specs/<YYYYMMDDHHmmss>-<name>.md` — timestamp local, **no separators**; name = kebab-case.

## Spec frontmatter

`artifact_type: spec`, `id`, `title`, `slug`, timestamps, `status`, `tags`, `feature_refs`, `ddd_refs`, `prd_refs`, `tech_refs`, `db_refs`, `github_issue`, `search_index`.

Optional: `plan_ready: true` after `/plan` (do not store plan content in the spec).

## Plan file

- **Same basename as spec** in `.claude/plans/`
- Frontmatter: `related_spec`, `branch`, `created_at`
- Task checklist with file paths — enough for `/build`
- Removed after successful `/build` (or manually)

## Git

Never `git add .claude/plans/` or plan files. Only `docs/specs/` specs are durable artifacts.
