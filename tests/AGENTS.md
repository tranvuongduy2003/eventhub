# EventHub backend testing instructions

## Scope

Applies to `tests/**` and backend test additions elsewhere. Inherits the root instructions.

## Test placement

- `tests/Domain.UnitTests`: aggregate behavior, value objects, state transitions, and high-risk `INV-*` rules.
- Application/component tests: handler orchestration, validation, authorization, idempotency decisions, and port fakes; follow the existing project layout rather than inventing a new test project.
- `tests/Api.IntegrationTests`: HTTP contracts, cookies/session, auth, EF mappings, PostgreSQL transactions/concurrency, infrastructure boundaries, and ProblemDetails.
- Consumer integration tests: Channel-based delivery, idempotency, retry behavior, projections, and notifications; follow the current repository layout.
- `tests/Testing.Common`: shared fixtures, fictional builders, assertions, fakes, clocks, and test infrastructure only.

## Rules

- Assert observable behavior and invariants, not private methods or implementation details.
- Mirror nearby fixture and assertion patterns before adding abstractions.
- Keep all data fictional; never use real credentials, tokens, attendee data, payment payloads, or full production-like secrets.
- Cover validation and expected failure paths when request handling changes.
- Use integration tests for status codes, ProblemDetails, cookies, persistence, concurrency, and real adapter behavior.
- Prefer real engines/Testcontainers when an in-process fake would hide transaction, SQL, cache, or object-storage behavior.
- Keep tests deterministic. Inject/control time; do not rely on wall-clock delays.

## Mandatory risk scenarios when affected

- two buyers race for the final ticket and exactly one succeeds;
- hold expiry releases inventory and cannot later confirm;
- duplicate payment callbacks capture/confirm at most once;
- duplicate order-confirmed delivery does not duplicate tickets;
- repeated check-in admits once and returns stable prior-success information;
- cancellation is safe under message redelivery;
- later price changes do not alter an existing order;
- Staff cannot perform Owner-only operations.

## Verification

Run the narrowest affected test project discovered in the repository, then:

```powershell
dotnet test EventHub.slnx -c Release
```
