# Link Issues, Project, Labels, and Assignees

## Link Issues to the PR

GitHub links issues from the PR description using closing keywords, which shows under Development on the issue.

| Mode | PR footer line | On merge to `main` |
|------|----------------|-------------------|
| Default (`link-closes`) | `Closes #123` | Issue auto-closes |
| `link-only` | `Refs #123` | Issue stays open as a mention only |

## Metadata Sync

After the PR is created, use GitHub MCP to apply metadata.

| Target | Action |
|--------|--------|
| **PR** | Add to Project, add labels, add assignees; set Status via `prStatusOnProject` |
| **Issues** | Add to Project if missing; set Status via `issueStatusOnPrCreated`; add labels and assignees |

### Config (`.github/github-project.json`)

```json
{
  "owner": "your-login",
  "projectNumber": 1,
  "projectTitle": "Your project name",
  "issueStatusOnPrCreated": "In review",
  "prStatusOnProject": "In review",
  "addPrToProject": true,
  "ensureIssuesOnProject": true,
  "inheritFromIssues": { "prLabels": true, "prAssignees": true },
  "prLabels": [],
  "prAssignees": ["@me"],
  "issueLabels": [],
  "issueAssignees": []
}
```

- **`inheritFromIssues`:** When linking issue #5, copy its labels/assignees onto the PR, unioned with `prLabels` / `prAssignees`.
- **`projectTitle`:** Must match the board name used by GitHub MCP when adding PRs and issues to the Project.
- **`ensureIssuesOnProject`:** Add linked issues to the Project through GitHub MCP when they are not on the board yet.

### Metadata Flags on create-pr

| Flag | Example |
|------|---------|
| `labels` | `labels enhancement,spec` |
| `assignee` | `assignee @me` or `assignee octocat` |
| `skip-metadata` | Skip metadata sync entirely |
| `skip-project-status` | Labels/assignees/project only; no Status field change |

Requires GitHub MCP tools with Project metadata capability.

### Status Field

- **`issueStatusOnPrCreated`** (alias `statusOnPrCreated`): linked issues when a PR is created.
- **`prStatusOnProject`**: PR card Status, e.g. `In review`. Omit to leave PR Status unchanged after adding the PR to the Project.

If a status name is missing on the board, add it under Project Settings > Status, or pick an existing option name.
