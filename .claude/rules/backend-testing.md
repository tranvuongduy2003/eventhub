---
description: "xUnit + FluentAssertions + Testcontainers testing conventions for EventHub. Use when writing or running tests in tests/ — covers domain unit test purity, integration test patterns, naming (Method_Scenario_Expected), and what not to test."
paths:
  - "tests/**"
---

# BACKEND TESTING

Source: [`docs/constitution.md`](../../docs/constitution.md) VII, [`docs/technical.md`](../../docs/technical.md) §11. Consult `CLAUDE.md` and `backend.md` first.

**No separate testing skill** — follow Constitution VII and mirror patterns in `tests/`.

## Tooling

- **xUnit** + **FluentAssertions**
- **Testcontainers** — PostgreSQL, Redis, RabbitMQ, MinIO in `Api.IntegrationTests` as needed
- **Fakes** at Application **ports** only — never mock domain aggregates

## Layout

```
tests/
├── Domain.UnitTests/           ← aggregates, value objects (see ddd.md)
└── Api.IntegrationTests/       ← HTTP + infrastructure wiring
e2e/                            ← Playwright e2e tests (see e2e-testing.md)
```

## Naming

`Method_Scenario_Expected`

## What to test (required focus)

| Area | Examples |
|------|----------|
| **Domain** | Aggregate invariants (`INV-*` in `ddd.md`), value object validation |
| **Integration** | HTTP flows per `features.md` acceptance criteria |

## What not to test

- Trivial getters/pass-through wiring
- EF configuration in isolation (covered by integration paths)
- Third-party frameworks (ASP.NET, MediatR internals)

## Domain tests are pure

```csharp
// construct aggregate directly, assert behavior/events
var order = Order.Place(...);
order.DomainEvents.Should().ContainSingle();

// no DI container, no EF, no mocks of domain types
```

## Helper methods

- No **ValueTuples** for test helper returns — define a `private sealed record` inside the test class (e.g. `EventSetup`, `EventData`).

## DON'TS

- No `Thread.Sleep` — use polling with timeout helpers
- No tests added only to raise coverage metrics
- No integration tests that depend on manual Docker state left from a dev machine
