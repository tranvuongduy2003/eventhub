---
name: create-pr
description: "End-to-end EventHub pull request workflow: inspect local changes, verify, write Conventional Commit messages, commit, push, draft a reviewer-friendly PR description, create the PR through GitHub MCP, link issues, and report metadata. Use when the user asks to commit, create PR, open pull request, ship changes, write PR description, or prepare a branch for review."
---

# Create Pull Request

Use this single skill for commit writing, PR description writing, and PR creation in EventHub. GitHub automation must use GitHub MCP only.

## Modes

| User intent | Do |
| --- | --- |
| "write a commit message" | Analyze staged diff and propose a Conventional Commit message. Commit only when asked. |
| "commit this" | Stage intentionally if requested, write the message, and commit. |
| "write a PR description" | Analyze `main...HEAD` and produce the PR body without pushing. |
| "create/open PR", "ship it" | Run the full workflow below. |

## Full Workflow

1. Inspect local state:

```powershell
git status --short
git branch --show-current
git diff --stat
git diff --cached --stat
```

2. Confirm the change is one logical PR. If staged changes mix unrelated work, suggest a split.
3. Run required verification. Prefer:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

For broader PR readiness, mirror relevant CI steps from `.github/workflows/ci.yml`:

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

4. Stage intentionally. Never stage `.env`, credentials, `.github/github-project.json`, build outputs, `web/src/generated/`, or `contracts/openapi/.build/`.
5. Write a Conventional Commit message from `git diff --cached`:
   - Format: `type(scope): imperative subject`
   - Types: `feat`, `fix`, `refactor`, `docs`, `style`, `test`, `chore`, `perf`, `ci`
   - Keep the subject under 72 characters, lowercase after the colon, no final period.
   - Add a body when the why is not obvious.
   - Add `BREAKING CHANGE:` footer for incompatible public changes.
6. Commit with normal hooks. Do not use `--no-verify`.
7. Push the branch:

```powershell
git push -u origin HEAD
```

8. Draft the PR description from `git diff main...HEAD --stat`, `git diff main...HEAD`, and `git log --oneline main..HEAD`.
9. Create the PR through GitHub MCP. If MCP tools are unavailable, stop before GitHub operations and report the missing capability.
10. Link issues when provided or found in commits. Use `Closes #N` by default; use `Refs #N` when the user asks for link-only.
11. Apply labels, assignees, Project, and Status only through GitHub MCP when requested/configured.

## PR Description Format

```markdown
## What

One concise paragraph describing the change.

## Why

The motivation, issue, bug, or workflow need.

## How

Key implementation choices in logical order, not a file list.

## Changes

- **Area**: Significant change.

## Testing

- [ ] Command or manual check and result.

## Screenshots

Include for UI changes, or state why none are needed.

## Notes for reviewers

Risk areas, deferred work, or files worth extra attention.
```

Keep small PRs proportionally short. Do not list generated files or formatting churn unless that is the point of the PR.

## GitHub Rules

- Use `github-mcp` for all repository, issue, pull request, workflow, branch, commit, review, label, assignee, and Project metadata operations.
- Do not fall back to `gh`, direct HTTP, or web scraping for GitHub automation in this repo.
- Ask explicit confirmation before destructive GitHub actions such as closing issues manually, deleting branches, merging PRs, cancelling workflows, or changing repository settings.

## OpenAPI and Frontend Notes

- If Api or Contracts changed, run `yarn --cwd web api:verify`.
- If `api:verify` fails because the committed contract is stale, run `yarn --cwd web api:export`, review and commit `contracts/openapi/api.v1.yaml`, then re-run verify.
- Do not commit `web/src/generated/`.

## Report

Report:

- Branch
- Commit hash and subject
- Verification commands and results
- PR URL
- Linked issues
- Labels, assignees, Project, and Status applied or skipped
- Residual risk or deferred manual checks
