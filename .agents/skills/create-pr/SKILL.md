---
name: create-pr
description: End-to-end pull request workflow: branch, local CI, Conventional Commits, push, open PR through GitHub MCP, optionally link GitHub issues, attach Project/labels/assignees on PR and issues, and set Project Status. Use when the user asks to create PR, open pull request, ship changes, run create-pr, or mentions attaching an issue to a PR.
---

# Create Pull Request

Orchestrates the full PR workflow for this repository. Delegates commit messaging to `git-commit-writer` and PR body to `pr-description-writer`; uses GitHub MCP for every GitHub operation.

## When To Use

- User says: create PR, open PR, ship it, branch and PR, create-pr
- User says: create PR for issue #N / attach issue / link issue
- After implementation is done and changes should land on `main` via review

Do not use for commit-only or description-only requests; use `git-commit-writer` or `pr-description-writer` alone.

## Input And Flags

Parse before Step 5.

**Issue numbers**

| Form | Example | Meaning |
|------|---------|---------|
| Issue number(s) | `5`, `#5`, `5,6`, `5 6` | Link issues; sync Project / labels / assignees / Status |
| Natural language | `create PR for issue 5` | Parse issue numbers from the message |

If omitted, Step 7 still runs PR-only metadata when configured (`addPrToProject`, `prLabels`, `prAssignees`). Otherwise Steps 1-5 only when there is nothing to sync.

**Flags**

| Flag | Default | Meaning |
|------|---------|---------|
| `link-closes` | yes | Append `Closes #N` |
| `link-only` | no | Append `Refs #N` instead |
| `skip-metadata` | no | Skip Step 7 entirely |
| `skip-project-status` | no | Step 7 without Status field change |
| `status <name>` | config or `In review` | Override Project Status option |
| `labels a,b` | none | Extra PR labels, comma-separated |
| `assignee x` | none | Extra PR assignee, e.g. `assignee @me` |
| `issue-labels a,b` | none | Extra labels on linked issues |
| `issue-assignee x` | none | Extra assignees on linked issues |

## Prerequisites

- Clean intent: all changes belong in one logical PR, otherwise split first
- On repo root; base branch is `main` unless user specifies another
- User explicitly requested commit/PR
- GitHub MCP tools are available for PR creation and metadata
- Issue linking + Project status: GitHub MCP Project metadata capability and [`.github/github-project.json`](../../.github/github-project.json), copied from [`.github/github-project.json.example`](../../.github/github-project.json.example)

## Workflow

Execute in order:

1. Branch, local CI, commit, push, create PR through GitHub MCP.
2. Append linked issues to PR body (`Closes #N` unless `link-only`).
3. Use GitHub MCP for metadata: PR to Project + labels + assignees; issues to board + Status + optional labels/assignees.

Report PR URL, linked issues, and what metadata was applied, skipped, or unavailable.

## Workflow Checklist

```text
- [ ] 1. Branch created from main
- [ ] 2. CI pipeline verified locally
- [ ] 3. Changes staged, no secrets
- [ ] 4. Commit using git-commit-writer
- [ ] 5. Push + PR using pr-description-writer + GitHub MCP
- [ ] 6. Link issues on PR, if issue numbers
- [ ] 7. Sync Project, labels, assignees, Status unless skip-metadata
```

## Step 1: Create Branch

1. `git fetch origin` and ensure `main` is current: `git checkout main` then `git pull origin main` if user allows pull.
2. Branch name: default prefix `feature/` unless the user gives another, such as `fix/` or `chore/`.
3. Use `feature/<short-kebab-slug>`, e.g. `feature/frontend-mvp-shell`.
4. If already on a non-`main` branch with the right work, reuse it.
5. Create and switch: `git checkout -b feature/<slug>`.

PowerShell: chain with `;` when needed.

## Step 2: Verify Pipeline

Run the same checks as [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml). Full command list: [references/ci-pipeline.md](references/ci-pipeline.md).

Mandatory default checks:

```powershell
dotnet restore EventHub.slnx
dotnet format EventHub.slnx --verify-no-changes
dotnet build EventHub.slnx --no-restore -c Release
dotnet test EventHub.slnx --no-build -c Release --verbosity normal

yarn --cwd web install --frozen-lockfile
yarn --cwd web api:verify
yarn --cwd web lint
yarn --cwd web format:check
yarn --cwd web build
```

`yarn api:verify` is mandatory when `src/Api/`, `src/Contracts/`, or OpenAPI-relevant code changed. If it fails, run `yarn --cwd web api:export`, commit the updated `contracts/openapi/api.v1.yaml`, then re-verify.

If `yarn format:check` fails, run `yarn --cwd web format`, then re-check.

Do not proceed to Step 3 until every required step exits 0. If any step fails, fix it and re-run the affected pipeline. Never open a PR with known red checks.

Scope: Skip frontend checks when the PR touches only backend/docs; skip .NET steps when the PR is frontend-only. Never skip `yarn api:verify` for API or Contracts changes.

## Step 3: Stage Changes

1. `git status`: review untracked and modified files.
2. Never stage `.env`, credentials, `**/bin/`, `**/obj/`, `.github/github-project.json`, or local IDE junk.
3. Stage intentionally with `git add <paths>` or `git add -A` after confirming no secrets.
4. If nothing to commit, stop and tell the user.

## Step 4: Commit

Read and follow [`.agents/skills/git-commit-writer/SKILL.md`](../git-commit-writer/SKILL.md):

1. `git diff --cached --stat` and analyze `git diff --cached`.
2. Write a Conventional Commits message: `type(scope): imperative subject` plus body if needed.
3. Commit, because the user invoked create-pr.

PowerShell commit:

```powershell
git commit -m "feat(scope): short subject" -m "Optional body paragraph."
```

Git safety: no `--no-verify`, no `git config` changes, no force-push to `main`, no amend unless user rules allow.

## Step 5: Push And Create PR

1. `git push -u origin HEAD`.
2. Read and follow [`.agents/skills/pr-description-writer/SKILL.md`](../pr-description-writer/SKILL.md) to draft the body from `git diff main...HEAD` and `git log --oneline main..HEAD`.
3. If issue numbers are known, include a **Linked issues** section in the initial body.
4. Use GitHub MCP to create the PR from the pushed branch into the base branch with the drafted title and body.
5. Capture PR number and URL from the MCP response.

PR title: Prefer the commit subject line; expand only if too vague.

## Step 6: Link Issues To The PR

Append to the PR description so GitHub shows the link under the issue Development panel.

Default (`link-closes`):

```markdown
## Linked issues

Closes #5
```

Alternate (`link-only`): use `Refs #5`, which does not auto-close on merge.

If the section was not in the initial body, use GitHub MCP to read the current PR body and update it with the linked-issues footer.

Epic + stories: pass every issue to link. Avoid closing an epic until all stories ship unless the user asks.

Details: [references/github-issue-link.md](references/github-issue-link.md).

## Step 7: Sync Project, Labels, Assignees, Status

Unless `skip-metadata`, use GitHub MCP after Step 5. Apply PR-only metadata even when no issues are linked.

| Target | Actions |
|--------|---------|
| **PR** | Add to Project using board title from config; add labels; add assignees |
| **Linked issues** | Add to Project if missing; set Status via `issueStatusOnPrCreated`; optionally add `issueLabels` / `issueAssignees` |
| **PR Status** | Set when `prStatusOnProject` is configured, often `In review` |

Config: copy [`.github/github-project.json.example`](../../.github/github-project.json.example).

- `addPrToProject`, `ensureIssuesOnProject` default to `true`
- `inheritFromIssues.prLabels` / `prAssignees` copy from linked issues onto the PR
- `prLabels`, `prAssignees`, `issueLabels`, `issueAssignees` are always applied when set

If Status option is missing on the board, warn user to add the column in Project settings or set `issueStatusOnPrCreated` / `prStatusOnProject` to existing option names.

Requires GitHub MCP Project metadata capability.

## Report To User

| Item | Value |
|------|-------|
| Branch | `feature/...` |
| Commit | hash + subject |
| Pipeline | pass/fail per step, brief |
| PR | URL |
| Linked issues | `#5` or `none` |
| PR project / labels / assignees | applied / skipped / unavailable |
| Issue project / labels / assignees / Status | per issue or skipped |

Mention deferred manual checks such as Aspire smoke test or screenshots in the PR Testing section, not as blockers unless CI failed.

## Related Skills

| Step | Skill |
|------|-------|
| Commit message | `git-commit-writer` |
| PR body | `pr-description-writer` |
| GitHub operations | `github-mcp` |
| Implement before PR | `cook` skill in [`.agents/skills/cook/SKILL.md`](../cook/SKILL.md) |
