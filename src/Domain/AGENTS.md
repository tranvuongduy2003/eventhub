# EventHub Domain instructions

## Scope

Applies to `src/Domain/**`. Inherits `src/AGENTS.md` and the root instructions.

## Domain purity

- Keep Domain pure C#. Do not reference EF Core, ASP.NET Core, MediatR, Redis, RabbitMQ, MinIO, SignalR, Infrastructure, or transport contracts.
- Do not add persistence annotations, serialization concerns, dependency-injection code, logging, HTTP concepts, or infrastructure retry logic.
- Reference other aggregates and bounded contexts by typed identity only.

## Modeling rules

- Aggregates are consistency boundaries. Applicable `INV-*` rules must hold at transaction commit.
- Put state transitions and invariant-preserving behavior on aggregate roots/entities/value objects; do not turn handlers into the primary home of domain logic.
- Value objects are immutable, self-validating, and compared by value.
- Domain events describe facts that already occurred. Keep them mechanism-free.
- Use a domain service only when domain logic genuinely spans concepts and does not naturally belong to one aggregate.
- Cross-aggregate workflow and external I/O belong to Application, not Domain.
- Preserve the ubiquitous language in `technical.md`; use one term per concept across code and tests.

## High-risk invariants

Changes touching inventory, orders, payment, tickets, or roles must explicitly account for the relevant invariants, including:

- `Reserved + Sold <= Capacity`;
- order totals and price snapshots do not change retroactively;
- invalid order/payment/ticket state transitions are impossible;
- a logical ticket is issued once and admitted once;
- transfer invalidates the old code and cannot introduce markup;
- exactly one Owner exists per event when event-scoped roles are implemented.

## Verification

Add or update focused domain unit tests for behavior, validation, state transitions, and every changed high-risk invariant. Prefer public behavior assertions over private-state or implementation-detail assertions.
