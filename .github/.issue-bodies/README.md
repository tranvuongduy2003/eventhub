# Issue body template (`/spec` Step 3)

Skeleton for **`gh issue create --body-file`** when running `/spec`.

**One issue per spec** — not epic + per-story issues.

| File | Use |
|------|-----|
| `spec.template.md` | Single tracking issue for the whole feature spec |

**Workflow**

1. Copy `spec.template.md` to a temp path.
2. Fill from `docs/_memory/specs/<YYYYMMDDHHmmss>-<feature>.md` (problem, AC list, links).
3. `gh issue create --title "Spec: …" --label "spec,enhancement" --body-file $path`
4. Set spec frontmatter `github_issue: <number>`.
