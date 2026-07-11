---
name: github-mcp
description: Use the configured GitHub MCP server for repository, issue, pull request, workflow, branch, commit, and code-search operations. Use for all GitHub automation in this repository.
---

# GitHub MCP

Use the `github` MCP server configured in `.codex/config.toml`. In this repository the active
configuration launches the GitHub MCP server through Docker and passes authentication from the
`GITHUB_PERSONAL_ACCESS_TOKEN` environment variable:

```toml
[mcp_servers.github]
command = "docker"
args = [
  "run",
  "-i",
  "--rm",
  "--name", "eventhub-github-mcp-server",
  "-e", "GITHUB_PERSONAL_ACCESS_TOKEN",
  "-e", "GITHUB_TOOLSETS=all",
  "ghcr.io/github/github-mcp-server"
]
env_vars = ["GITHUB_PERSONAL_ACCESS_TOKEN"]
```

## Priority

1. Use GitHub MCP tools for all GitHub automation.
2. Do not use command-line or direct HTTP GitHub surfaces for GitHub operations.
3. Never commit GitHub tokens or OAuth values. The token must remain in the environment and must not be copied into `.codex/config.toml`, `.mcp.json`, docs, logs, or reports.

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
