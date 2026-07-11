# Risk-Based Coverage

Use this to choose verification depth. `AGENTS.md`, `tests/AGENTS.md`, and `docs/technical.md` own mandatory risk scenarios.

## Add Or Update Tests When

- A domain invariant changes.
- A validator changes.
- An endpoint status code or response shape changes.
- Session/auth behavior changes.
- Persistence mapping or query behavior changes.
- Object storage or cache/session failure behavior changes.

## Choose The Lowest Useful Level

- Domain unit tests for aggregate/value-object behavior and invariant enforcement.
- Application/component tests for orchestration, validation, authorization, idempotency decisions, and ports.
- API integration tests for HTTP contracts, ProblemDetails, sessions, persistence, transactions, concurrency, and real adapter behavior.
- E2E tests only when browser navigation or a cross-page workflow is the acceptance surface.

## Verification Ladder

Use focused tests while iterating, then broaden:

```powershell
dotnet test tests/Domain.UnitTests/EventHub.Domain.UnitTests.csproj -c Release
dotnet test tests/Api.IntegrationTests/EventHub.Api.IntegrationTests.csproj -c Release
dotnet test EventHub.slnx -c Release
```
