# REST API contract (OpenAPI)

The committed source of truth for HTTP endpoints is **`api.v1.yaml`**. The React app consumes TypeScript types generated from this file; generated types are **not** committed (see `web/src/generated/`).

## Workflow

When you change REST endpoints or response shapes in `EventHub.Api`:

1. **Regenerate the document** (builds the API and refreshes the YAML):

   ```bash
   yarn --cwd web api:export
   ```

2. **Regenerate frontend types** (also runs automatically before `yarn dev` / `yarn build`):

   ```bash
   yarn --cwd web api:codegen
   ```

3. **Commit** `contracts/openapi/api.v1.yaml` only (not `web/src/generated/`).

Intermediate JSON from MSBuild is written to `contracts/openapi/.build/` (gitignored).

## Verify without writing (CI / local)

```bash
yarn --cwd web api:verify
```

Fails if the API build produces a different contract than the committed YAML.

## Tooling

| Step | Tool |
|------|------|
| OpenAPI from C# | `Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server` (`OpenApiGenerateDocuments=true` on build) |
| JSON → YAML | `scripts/openapi/sync-contract.mjs` (uses `yaml` from `web`) |
| YAML → TypeScript | [`openapi-typescript`](https://github.com/drwpow/openapi-typescript) |

Agent workflow: `.cursor/skills/openapi-contract-sync/SKILL.md`.
