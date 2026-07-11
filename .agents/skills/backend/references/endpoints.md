# Endpoint Workflow

Use this for changes under `src/Api/Endpoints`, HTTP mapping, ProblemDetails, or public REST contracts. `src/Api/AGENTS.md`, `src/Contracts/AGENTS.md`, and `contracts/AGENTS.md` own the durable rules.

## Scout

```powershell
rg -n "class .*Endpoint|IEndpoint|MapGroup|Map(Post|Get|Put|Patch|Delete)" src/Api/Endpoints
rg -n "ToHttpResult|ProblemDetails|ApiProblemDetails|InvalidRequestProblems" src/Api
rg -n "record .*Request|record .*Response|class .*Request|class .*Response" src/Contracts
rg -n "operationId:|/api/" contracts/openapi/api.v1.yaml
```

Open a nearby endpoint with the same auth, binding, and result shape. Mirror its route grouping, filters, typed results, metadata, and tests.

## Change Flow

1. Read the relevant API, Contracts, contracts, and tests instructions.
2. Reconcile the behavior with the applicable `F-*` acceptance criteria.
3. Update runtime endpoint mapping and Contracts DTOs together.
4. Use `openapi-contract-sync` for public REST shape changes.
5. Add or update integration tests for the behavior users or clients observe.

Typical contract commands:

```powershell
yarn --cwd web api:export
yarn --cwd web api:codegen
yarn --cwd web api:verify
```

Let `openapi-contract-sync` decide which generated artifacts are ignored versus committed.
