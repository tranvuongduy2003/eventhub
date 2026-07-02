---
name: neo4j-graphrag
description: Query Neo4j via GraphRAG MCP â€” Cypher, vector search, fulltext search, schema inspection. Use when exploring graph data, domain relationships, or RAG retrieval against Neo4j.
---

# Neo4j GraphRAG MCP

Inspect and query **Neo4j** through the **mcp-neo4j-graphrag** MCP server configured in `.codex/config.toml`.

## When to use

- Explore **graph relationships** (bounded contexts, feature dependencies, traceability)
- Run **read/write Cypher** against a local or remote Neo4j instance
- **Vector** or **fulltext** search over ingested documentation nodes
- Verify **schema and indexes** before ingestion queries

## When not to use

- **PostgreSQL application state** â€” use `postgres-mcp` instead
- **Production secrets** â€” never commit credentials; use local environment variables or a local-only `.mcp.json` override outside the repo standard

## Prerequisites

1. Copy [`.env.example`](../../../.env.example) â†’ `.env` and set:
   - `NEO4J_URI`, `NEO4J_USERNAME`, `NEO4J_PASSWORD`, `NEO4J_DATABASE`
   - `OPENAI_API_KEY` (or another provider per server docs) for embeddings
2. **Install the server:**

```powershell
pip install mcp-neo4j-graphrag
```

Alternative (if [uv](https://docs.astral.sh/uv/) is installed): `uvx mcp-neo4j-graphrag`

The shared Codex config invokes the package via `python -c "from mcp_neo4j_graphrag import main; main()"`.

3. **Neo4j running** â€” Docker example:

```powershell
docker run -d --name neo4j `
  -p 7474:7474 -p 7687:7687 `
  -e NEO4J_AUTH=neo4j/neo4j `
  neo4j:5
```

4. Reload Codex after changing `.codex/config.toml` or local environment values.

## MCP config (`.codex/config.toml`)

```toml
[mcp_servers.neo4j-graphrag]
command = "python"
args = ["-c", "from mcp_neo4j_graphrag import main; main()"]

[mcp_servers.neo4j-graphrag.env]
NEO4J_URI = "bolt://localhost:7687"
NEO4J_USERNAME = "neo4j"
NEO4J_PASSWORD = "neo4j"
NEO4J_DATABASE = "neo4j"
OPENAI_API_KEY = ""
EMBEDDING_MODEL = "text-embedding-3-small"
```

Use values from `.env`; do not commit real keys.

## Key tools (typical)

| Tool | Use |
|------|-----|
| `read_neo4j_cypher` | Read-only Cypher |
| `write_neo4j_cypher` | Mutations (confirm with user before destructive writes) |
| `get_neo4j_schema_and_indexes` | Labels, relationships, indexes |
| `vector_search` | Semantic retrieval |
| `fulltext_search` | Keyword retrieval |
| `search_cypher_query` | Natural-language â†’ Cypher assist |

Exact tool names may vary by package version â€” list tools from the MCP connection if a call fails.

## EventHub graph ideas

Useful node types to ingest from docs (optional project setup):

- `Feature` (`F-*`), `Epic` (`EP-*`), `Aggregate` (`AGG-*`), `BoundedContext` (`BC-*`)
- Relationships: `DEPENDS_ON`, `REALIZES`, `OWNS`, `REFERENCES`

Source documents: [`docs/_memory/source/feature-specification.md`](../../../docs/_memory/source/feature-specification.md), [`docs/_memory/source/domain-model-specification.md`](../../../docs/_memory/source/domain-model-specification.md).

## Safety

- Prefer **read** tools for exploration
- Ask before **write** Cypher that deletes or bulk-updates
- Graph is **documentation/traceability** â€” PostgreSQL remains authoritative for runtime state (`constitution.md` III)

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| MCP server not listed | Reload Codex; verify `.codex/config.toml` contains `[mcp_servers.neo4j-graphrag]` |
| `uvx` not found | Use `pip install mcp-neo4j-graphrag` and the `python -c` command in `.codex/config.toml` |
| Connection refused | Start Neo4j; check `NEO4J_URI` port 7687 |
| Embedding errors | Set `OPENAI_API_KEY` or switch `EMBEDDING_MODEL` to local Ollama per upstream docs |

## References

- Upstream: [neo4j-field/mcp-neo4j-graphrag](https://github.com/neo4j-field/mcp-neo4j-graphrag)
- Aspire MCP: `aspire.md`
- Postgres MCP: `postgres-mcp` skill
