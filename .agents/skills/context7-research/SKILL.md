---
name: context7-research
description: >
  Research up-to-date documentation for any library, framework, technology, or programming language using Context7 MCP.
  Use when asked about library APIs, usage patterns, code examples, language version features, or "what's new" questions.
  Triggers on: (1) API/usage - "How do I use useEffect?", "Playwright locator API";
  (2) Version features - "what's new in C# 14", "Python 3.12 features", "TypeScript 5.0 changes";
  (3) Migration - "How to migrate from React 17 to 18?";
  (4) Breaking changes - "Breaking changes in Next.js 14";
  (5) Deprecations - "Is componentWillMount deprecated?";
  (6) Configuration - "How to configure ESLint for TypeScript?";
  (7) Integration - "How to use Zod with React Hook Form?";
  (8) Error troubleshooting - "How to fix hydration error in React?";
  (9) Method signatures - "What parameters does useQuery accept?";
  (10) Best practices - "Best practices for React Query caching";
  (11) Comparisons - "Difference between useMemo and useCallback".
---

# Context7 Research

Look up current documentation for any library, framework, technology, or programming language using Context7 MCP tools.

## Workflow

### 1. Identify Library/Language and Question

Extract from user query:
- **Library/Language name**: The framework, package, or language (e.g., "React", "Playwright", "C#", "Python")
- **Specific question**: What they need to know (e.g., "form validation", "page locators", "new features in version X")

### 2. Resolve Library ID

Call `mcp__context7__resolve-library-id`:

```
libraryName: extracted library name
query: the user's original question (helps rank results)
```

**Selection criteria** (in order):
1. Exact name match
2. Highest benchmark score
3. Most code snippets available

**If ambiguous**: Pick the most relevant match based on context. Common naming patterns:

| User says | Try resolving as |
|-----------|------------------|
| Language versions (C# 14, Python 3.12) | Language name (dotnet, python) |
| Framework + version (React 19, .NET 10) | Framework name (react, dotnet) |
| Abbreviated names (TS, EF, RHF) | Full name (typescript, efcore, react-hook-form) |
| Alternative names (Golang) | Canonical name (go) |

**Tip**: If the first resolution fails, try alternative names or search with the full library name.

### 3. Query Documentation

Call `mcp__context7__query-docs`:

```
libraryId: resolved ID from step 2
query: specific, detailed question
```

**Query tips**:
- Be specific: "cleanup function pattern" not "hooks"
- Include context: "JWT authentication middleware"
- Ask for examples: "mutation with optimistic updates example"
- For version features: "new features", "what's new in version X"
- For migrations: "migration guide from version X to Y"

### 4. Synthesize Response

Format the answer:

```markdown
[Concise explanation addressing the user's question]

[Code example if applicable]

**Source**: {library name} via Context7
```

## Constraints

- **Max 3 Context7 calls per question** - If not found after 3 attempts, use best available result
- **No sensitive data in queries** - Never include API keys, passwords, or credentials

## Edge Cases

| Situation | Action |
|-----------|--------|
| Library/language not found | Try alternative names, then use WebSearch as fallback |
| Multiple versions | Prefer latest stable unless user specifies version |
| Vague question | Ask user to clarify before querying |
| No useful results | Summarize what was found, suggest refining the query or use WebSearch |






