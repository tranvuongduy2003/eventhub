---
name: github-mcp
description: Use the GitHub remote MCP server for repository, issue, pull request, workflow, branch, commit, and code-search operations. Use when the user mentions GitHub MCP, remote GitHub MCP, GitHub issues, PRs, review threads, Actions, repository metadata, or wants GitHub data through the configured MCP server instead of gh CLI.
---

# GitHub MCP

Use the `github` MCP server configured in `.mcp.json`:

```json
{
  "mcpServers": {
    "github": {
      "type": "http",
      "url": "https://api.githubcopilot.com/mcp/"
    }
  }
}
```

## Priority

1. Prefer GitHub MCP tools when they are available in the active tool list.
2. Use `github-cli` / `gh` only as a fallback when MCP tools are not exposed, a workflow needs a `gh`-only feature, or the user explicitly asks for CLI.
3. Never put GitHub tokens or OAuth values in `.mcp.json`; the remote MCP handles authentication through the host client.

## Before Acting

- Confirm repository context from local git first:

```powershell
git remote -v
git branch --show-current
```

- If GitHub MCP tools are missing from the session, say the `github` MCP server is configured but not currently exposed, then use `gh` only if authenticated and appropriate.
- For destructive actions such as closing issues, deleting branches, merging PRs, cancelling workflows, or changing repository settings, ask for explicit confirmation unless the user already gave that exact instruction.

## Common Workflows

### Repository and Code Search

Use MCP for repository metadata, file reads, branch lists, commits, and GitHub code search. Prefer structured MCP responses over scraping web pages.

### Issues

Use MCP to list, inspect, create, edit, label, assign, and comment on issues. Before creating an issue, search for duplicates in the target repository.

### Pull Requests

Use MCP to inspect PR metadata, changed files, checks, commits, reviews, comments, and review threads. For code review tasks, combine MCP PR context with local `git diff` when the branch is checked out locally.

### GitHub Actions

Use MCP to inspect workflow runs, jobs, logs, and check conclusions. If logs are large, summarize failing job names and the smallest relevant error lines.

## Output Rules

- Include repository owner/name and concrete issue/PR/run numbers when reporting results.
- Distinguish live GitHub state from local git state.
- Do not invent GitHub state if MCP/CLI access is unavailable.
