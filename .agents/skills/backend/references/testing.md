# Backend Testing Workflow

Use this after reading `tests/AGENTS.md`. Let nearby tests decide fixture and assertion style.

## Scout

```powershell
rg -n "class .*Tests|Fact\]|Theory\]|IntegrationTestFixture|TestClock" tests
rg -n "AssertValidationFailedAsync|ShouldBeSuccess|ShouldBeFailure|ApiProblemDetails" tests
rg -n "WithWebHostBuilder|RemoveAll<|Testcontainers|Respawn" tests
```

## Placement

- Put aggregate, value-object, validator, and invariant behavior in the narrowest domain/application test project that already covers similar behavior.
- Put HTTP status, cookies/session, ProblemDetails, EF mapping, transaction, cache/storage, and provider-bound behavior in API integration tests.
- Put shared builders, fake adapters, test clocks, and fixture helpers in `tests/Testing.Common` only when multiple tests need them.

## Flow

1. Find a nearby test for the same feature or layer.
2. Start with the externally visible behavior or invariant.
3. Cover expected failure paths when validation, auth, conflict, or error mapping changes.
4. Keep fixtures fictional and deterministic.
5. Run the narrowest affected test project, then the changed-code verifier when handing off.
