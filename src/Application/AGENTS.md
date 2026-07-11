# EventHub Application instructions

## Scope

Applies to `src/Application/**`. Inherits `src/AGENTS.md` and the root instructions.

## Ownership

Application owns commands, queries, handlers, validators, pipeline behaviors, application services, repository/external-system ports, identity access, and cross-context orchestration.

## CQRS and flow

- Commands implement `ICommand` / `ICommand<T>` and may mutate state.
- Queries implement `IQuery<T>` and must be side-effect free.
- Handlers return `Result` / `Result<T>` for expected outcomes.
- Validate at the application boundary before state mutation.
- Use `ICurrentUserAccessor` and perform event-scoped authorization for protected operations.
- Use `IClock` for time-dependent behavior.
- Keep handlers orchestration-focused. Invoke aggregate behavior instead of duplicating invariants.
- Define ports here; implementations belong in Infrastructure.

## Pipeline contract

Do not casually reorder or bypass the documented MediatR pipeline:

1. logging/correlation;
2. validation;
3. post-commit session-cache update;
4. unit of work and bounded optimistic-concurrency retry;
5. in-process domain-event dispatch before commit;
6. handler.

Queries bypass transaction/retry behavior unless a documented consistent snapshot is required. Domain-event handlers inside the unit of work must be local, deterministic, and free of slow external side effects.

## Consistency and idempotency

- The reservation plus order-placement operation is the documented cross-aggregate transactional exception; do not generalize it.
- Persist integration events with the state change that produced them and publish after commit.
- Make webhook, consumer, ticket-issuance, check-in, refund, cancellation, and notification decisions safe under retries.
- Do not use cache state as authoritative input for correctness decisions.

## Verification

Use application unit/component tests for handler orchestration, validation, authorization, idempotency decisions, and port interactions. Add API/integration coverage when correctness depends on transactions, EF mappings, real engines, or transport behavior.
