---
name: github-mcp
description: Use the GitHub remote MCP server for repository, issue, pull request, workflow, branch, commit, and code-search operations. Use for all GitHub automation in this repository.
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

1. Use GitHub MCP tools for all GitHub automation.
2. Do not use command-line or direct HTTP GitHub surfaces for GitHub operations.
3. Never put GitHub tokens or OAuth values in `.mcp.json`; the remote MCP handles authentication through the host client.

## Before Acting

- Confirm repository context from local git first:

```powershell
git remote -v
git branch --show-current
```

- If GitHub MCP tools are missing from the session, stop the GitHub operation and report that the MCP capability is unavailable.
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
- Do not invent GitHub state if MCP access is unavailable.
