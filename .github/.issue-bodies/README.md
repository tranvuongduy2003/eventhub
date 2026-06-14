# Issue body templates (`/spec` Step 3)

Canonical markdown skeletons for **`gh issue create --body-file`** when running the `/spec` command.

Agents must **fill every section** from the spec artifact — do not commit filled copies here (only these templates stay in git).

| File | Use |
|------|-----|
| `epic.template.md` | Parent tracking issue (one per feature) |
| `story.template.md` | One issue per §2 story |

**Workflow**

1. Copy template to a temp path (e.g. `$env:TEMP\gh-epic.md`).
2. Replace all `<placeholders>` with content from `docs/specs/<timestamp>-<feature>.md`.
3. `gh issue create --body-file $path ...`
4. Delete the temp file.

**Related:** [`.github/ISSUE_TEMPLATE/`](../ISSUE_TEMPLATE/) — same structure for manual “New issue” in GitHub UI.
