---
name: create-pr
description: End-to-end pull request workflow â€” branch, local CI, Conventional Commits, push, open PR, optionally link GitHub issues (Closes #N), attach Project/labels/assignees on PR and issues, and set Project Status. Use when the user asks to create PR, open pull request, ship changes, run create-pr, or mentions attaching an issue to a PR.
---

# Create Pull Request

Orchestrates the full PR workflow for this repository. Delegates commit messaging to `git-commit-writer` and PR body to `pr-description-writer`; uses `github-cli` for `gh pr create`.

## When to use

- User says: create PR, open PR, ship it, branch and PR, create-pr
- User says: create PR **for issue #N** / attach issue / link issue
- After implementation is done and changes should land on `main` via review

**Do not use** for commit-only or description-only requests â€” use `git-commit-writer` or `pr-description-writer` alone.

## Input and flags (parse from user message)

Parse **before** Step 5.

**Issue numbers**

| Form | Example | Meaning |
|------|---------|---------|
| Issue number(s) | `5`, `#5`, `5,6`, `5 6` | Link issues; sync Project / labels / assignees / Status |
| Natural language | `create PR for issue 5` | Parse issue numbers from the message |

If omitted, Step 7 still runs **PR-only** metadata when configured (`addPrToProject`, `prLabels`, `prAssignees`). Otherwise Steps 1â€“5 only when there is nothing to sync.

**Flags**

| Flag | Default | Meaning |
|------|---------|---------|
| `link-closes` | **yes** | Append `Closes #N` (closes on merge to `main`) |
| `link-only` | no | Append `Refs #N` instead (e.g. "link only don't close") |
| `skip-metadata` | no | Skip Step 7 entirely |
| `skip-project-status` | no | Step 7 without Status field change |
| `status <name>` | config or `In review` | Override Project Status option |
| `labels a,b` | â€” | Extra PR labels, comma-separated |
| `assignee x` | â€” | Extra PR assignee (e.g. `assignee @me`) |
| `issue-labels a,b` | â€” | Extra labels on linked issues |
| `issue-assignee x` | â€” | Extra assignees on linked issues |

## Prerequisites

- Clean intent: all changes belong in **one logical PR** (otherwise split first)
- `gh` installed and authenticated (`gh auth status`)
- On repo root; base branch is **`main`** unless user specifies another
- User explicitly requested commit/PR (project rule: no surprise commits)
- **Issue linking + Project status:** `gh auth refresh -h github.com -s read:project,project` and [`.github/github-project.json`](../../.github/github-project.json) (copy from [`github-project.json.example`](../../.github/github-project.json.example))

## Workflow

Execute in order:

1. Steps 1â€“5 â€” branch â†’ CI â†’ commit â†’ push â†’ `gh pr create`.
2. **Step 6** â€” append linked issues to PR body (`Closes #N` unless `link-only`).
3. **Step 7** â€” `sync-pr-github-metadata.ps1`: PR â†’ Project + labels + assignees; issues â†’ board + Status + optional labels/assignees.

Report PR URL, linked issues, and what metadata was applied (or skipped / failed).

## Workflow checklist

```
- [ ] 1. Branch created from main
- [ ] 2. CI pipeline verified locally
- [ ] 3. Changes staged (no secrets)
- [ ] 4. Commit (git-commit-writer)
- [ ] 5. Push + PR (pr-description-writer + gh)
- [ ] 6. Link issues on PR (if issue numbers)
- [ ] 7. Sync Project, labels, assignees, Status (unless skip-metadata)
```

---

## Step 1: Create branch

1. `git fetch origin` and ensure `main` is current: `git checkout main` then `git pull origin main` (if user allows pull).
2. **Branch name** â€” default prefix `feature/` unless the user gives another (`fix/`, `chore/`):
   - `feature/<short-kebab-slug>` â€” e.g. `feature/frontend-mvp-shell`
   - Slug from plan header `> Branch: feature/...`, task title, or primary change area
3. If already on a non-`main` branch with the right work, **reuse it**; do not recreate.
4. Create and switch: `git checkout -b feature/<slug>`

**PowerShell:** chain with `;` not `&&`.

---

## Step 2: Verify pipeline

Run the same checks as [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml). Full command list: [references/ci-pipeline.md](references/ci-pipeline.md).

**MANDATORY — run all steps sequentially, verify each exits 0 before proceeding:**

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

**`yarn api:verify` is MANDATORY even for backend-only changes** — API endpoint changes modify the OpenAPI contract. If it fails: run `yarn --cwd web api:export`, commit the updated `contracts/openapi/api.v1.yaml`, then re-verify.

**If `yarn format:check` fails:** run `yarn --cwd web format`, then re-check.

**If `dotnet format` fails on Windows (CRLF):** CI runs on Ubuntu with LF â€” note in PR Testing section; fix only if the **changed** files fail on CI, not pre-existing tree-wide CRLF noise.

**GATE: Do not proceed to Step 3 until every step above exits 0.** If any step fails, fix it and re-run the full pipeline. Never open a PR with known red checks.

**Scope:** Skip `yarn lint`/`format:check`/`build` when the PR touches only backend/docs; skip .NET steps when the PR is frontend-only. **Never skip `yarn api:verify`** when `src/Api/` or `src/Contracts/` changed.

---

## Step 3: Stage changes

1. `git status` â€” review untracked and modified files.
2. **Never stage:** `.env`, credentials, `**/bin/`, `**/obj/`, `.github/github-project.json`, local IDE junk.
3. Stage intentionally: `git add` paths or `git add -A` after confirming no secrets.
4. If nothing to commit, stop and tell the user.

---

## Step 4: Commit

**Read and follow** [`.agents/skills/git-commit-writer/SKILL.md`](../git-commit-writer/SKILL.md):

1. `git diff --cached --stat` and analyze `git diff --cached`
2. Write Conventional Commits message (`type(scope): imperative subject` + body if needed)
3. Commit (user already invoked create-pr, so committing is in scope)

**PowerShell commit** (no bash heredoc):

```powershell
git commit -m "feat(scope): short subject" -m "Optional body paragraph."
```

**Git safety:** no `--no-verify`, no `git config` changes, no force-push to `main`, no amend unless user rules allow.

---

## Step 5: Push and create PR

1. `git push -u origin HEAD`
2. **Read and follow** [`.agents/skills/pr-description-writer/SKILL.md`](../pr-description-writer/SKILL.md) to draft the body from `git diff main...HEAD` and `git log --oneline main..HEAD`.
3. If issue numbers are known, include a **Linked issues** section in the initial body (see Step 6).
4. Create PR:

```powershell
gh pr create --base main --title "<subject>" --body-file $env:TEMP\pr-body.md
```

5. Capture PR number from output URL.

**PR title:** Prefer the commit subject line; expand only if too vague.

---

## Step 6: Link issues to the PR (when issue numbers provided)

Append to the PR description so GitHub shows the link under the issue **Development** panel.

**Default (`link-closes`):**

```markdown
## Linked issues

Closes #5
```

**Alternate (`link-only`):** use `Refs #5` (does not auto-close on merge).

If the section was not in the initial body, patch after create:

```powershell
$pr = 9   # from gh pr create output
$existing = gh pr view $pr --json body --jq .body
$footer = "## Linked issues`n`nCloses #5"
gh pr edit $pr --body ($existing.TrimEnd() + "`n`n" + $footer)
```

**Epic + stories:** pass every issue to link (e.g. `5` for the story in progress; avoid `Closes #4` on the epic until all stories ship unless the user asks).

Details: [references/github-issue-link.md](references/github-issue-link.md).

---

## Step 7: Sync Project, labels, assignees, Status

Unless `skip-metadata`, run from repo root after Step 5 (capture `$pr` from `gh pr create`):

```powershell
$params = @{
    PrNumber = $pr
    IssueNumber = @(5)   # omit @() when no issues; still syncs PR-only metadata
}
if ($userLabels) { $params.ExtraPrLabels = $userLabels -split ',' }
if ($userAssignees) { $params.ExtraPrAssignees = @($userAssignee) }
if ($skipProjectStatus) { $params.SkipProjectStatus = $true }
if ($statusOverride) { $params.StatusName = $statusOverride }

.agents/skills/create-pr/scripts/sync-pr-github-metadata.ps1 @params
```

**What the script does** (see [references/github-issue-link.md](references/github-issue-link.md)):

| Target | Actions |
|--------|---------|
| **PR** | `--add-project` (board title from config), `--add-label`, `--add-assignee` |
| **Linked issues** | `gh project item-add` if missing; Status â†’ `issueStatusOnPrCreated` (issues only); optional `issueLabels` / `issueAssignees` |
| **PR Status** | Set when `prStatusOnProject` is configured (often same column as the linked issue, e.g. `In review`) |

**Config** â€” copy [`.github/github-project.json.example`](../../.github/github-project.json.example):

- `addPrToProject`, `ensureIssuesOnProject` (default `true`)
- `inheritFromIssues.prLabels` / `prAssignees` â€” copy from linked issues onto the PR
- `prLabels`, `prAssignees`, `issueLabels`, `issueAssignees` â€” always applied when set

**If Status option missing** on the board: warn user to add the column in Project settings or set `issueStatusOnPrCreated` / `prStatusOnProject` to existing option names.

**Requires** `project` scope on `gh auth`.

Legacy wrapper (status only): `set-project-issue-status.ps1` delegates to this script using the current branch PR.

---

## Report to user

| Item | Value |
|------|--------|
| Branch | `feature/...` |
| Commit | hash + subject |
| Pipeline | pass/fail per step (brief) |
| PR | URL |
| Linked issues | `#5` â€¦ or `none` |
| PR project / labels / assignees | applied / skipped |
| Issue project / labels / assignees / Status | per issue or skipped |

Mention deferred manual checks (Aspire smoke test, screenshots) in the PR Testing section, not as blockers unless CI failed.

---

## Related skills

| Step | Skill |
|------|--------|
| Commit message | `git-commit-writer` |
| PR body | `pr-description-writer` |
| `gh` commands | `github-cli` |
| Implement before PR | `cook` skill in [`.agents/skills/cook/SKILL.md`](../cook/SKILL.md) |
