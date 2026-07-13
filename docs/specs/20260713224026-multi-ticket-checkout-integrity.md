---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260713224026-multi-ticket-checkout-integrity
title: Multi-Ticket Checkout Integrity
status: proposed
last_updated: 2026-07-13
owner: builder
language: en
applies_to:
  - F-3.4
  - F-3.6
  - F-5.3
  - F-5.5
  - F-6.2
  - F-6.3
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# Multi-Ticket Checkout Integrity

## Problem

One guest order may contain several ticket types and therefore several Event reservations. The
current paid-confirmation path commits only the primary `Order.ReservationId`, leaving later
reservations permanently reserved after a successful payment. This corrupts sold/available counts
and violates the no-oversell guarantee.

The current placement path aggregates duplicate ticket-type lines for availability but applies a
per-order limit to each raw line. An API caller can split a quantity across duplicate lines and
bypass F-3.6. Existing browser UI does not create duplicate lines, but server-side enforcement must
remain authoritative.

## Solution

Treat an order's reservation set as the reservations on its Event whose `OrderId` matches the order,
not as the single compatibility pointer on `Order.ReservationId`. Every terminal reservation path
must operate on the complete set: free confirmation, paid confirmation, expiry, and any
order-confirmation event fallback.

Make the Event aggregate's existing optimistic-concurrency token advance on every persistence
update. A retry must discard stale EF tracking and queued domain/realtime work before re-running
the command, so a final-ticket race re-reads inventory rather than reusing a failed attempt's state.

Normalize incoming placement lines by ticket type before applying availability, per-order limits,
reservation creation, and order-line persistence. One logical ticket type becomes one order line and
one reservation with the total requested quantity. The public checkout contract stays stable;
duplicate API input is handled safely rather than becoming a fairness bypass.

## Source Alignment

| Source | Requirement | Repair |
|---|---|---|
| `product.md` QG-5, RSK-5 | The product never sells the same seat twice and must work correctly at small scale. | Commit/release every reservation for a multi-ticket order in the same transaction. |
| `features.md` F-3.4 | Availability is `capacity - reserved - sold`; racing buyers cannot oversell. | No confirmed paid order may leave stale reserved inventory. |
| `features.md` F-3.6 | An order exceeding a configured ticket-type limit is blocked. | Enforce the limit against the aggregate quantity for each ticket type. |
| `features.md` F-5.3/F-5.5 | Orders create finite inventory holds that later release. | Model all reservations belonging to the order as its live hold set. |
| `features.md` F-6.2/F-6.3 | Free and paid confirmation are safe and idempotent. | Free and paid paths commit every reservation exactly once; duplicate callbacks do not re-commit. |
| `technical.md` INV-10, INV-21, INV-24, ARCH-8, section 10 | Inventory is transactional, commands are retry-safe, and quantities respect limits. | Use one authoritative event aggregate and idempotent terminal paths. |

## In Scope

- Aggregate same-ticket-type request lines before server-side validation and persistence.
- Enforce `MaxPerOrder` against each aggregated ticket-type quantity.
- Reserve exactly one aggregate quantity per ticket type for an order.
- Commit every live reservation belonging to a paid confirmed order.
- Preserve/verify full reservation release on hold expiry and align fallback domain-event handlers so
  no path reverts to primary-reservation-only behavior.
- Ensure ticket issuance independently reconciles residual confirmed-order reservations before it
  issues tickets, so handler enumeration order cannot determine inventory correctness.
- Add focused API integration evidence for multi-ticket paid confirmation, duplicate payment
  notifications, aggregate per-order limits, and multi-ticket expiry.

## Out of Scope

- Guest access capability hardening, QR rendering, or payment-webhook signature verification.
- Changing the public checkout request or response schema solely to expose internal reservation ids.
- A new migration: the existing reservation table already stores `OrderId`; the repair must not add
  redundant persistence merely to duplicate this relation.
- Cross-order attendee/email purchase limits, waitlists, dynamic pricing, or changes to discount
  policy.
- Browser UI redesign: the existing UI generates one quantity per ticket type, and the risk is
  server-side transaction correctness. Browser coverage is not a substitute for integration tests.

## Acceptance Criteria

1. **Aggregate per-ticket-type limits**

   GIVEN a ticket type has `MaxPerOrder = 1`, WHEN a caller submits two lines for that same type
   with quantity `1` each, THEN placement is rejected with the existing clear limit error; no order,
   reservation, discount usage, or inventory mutation is persisted.

2. **Safe duplicate-line normalization**

   GIVEN a caller submits repeated lines for the same ticket type whose aggregate quantity is within
   availability and the per-order limit, WHEN the order is placed, THEN it succeeds with one logical
   order line and one reservation for the aggregate quantity; price snapshots and totals remain
   correct.

3. **Independent multi-type limits**

   GIVEN an order contains several ticket types, WHEN each aggregate quantity is within that type's
   limit, THEN the order may place; WHEN any aggregate quantity exceeds its own limit, THEN the
   whole placement fails atomically.

4. **Paid multi-ticket confirmation**

   GIVEN a paid pending order has reservations for two or more ticket types, WHEN a valid provider
   success notification confirms payment, THEN every reservation for that order is committed, every
   affected ticket type moves the reserved quantity to sold, no reservation row remains for that
   order, and the order's compatibility reservation pointer is cleared.

5. **Exactly-once paid confirmation**

   GIVEN the same provider success notification is processed more than once for a multi-ticket
   order, WHEN the duplicate arrives, THEN the first result commits all reservations and the later
   result reports no new application; sold/reserved counts and tickets do not change again.

6. **Free multi-ticket confirmation**

   GIVEN a zero-total order contains several ticket types, WHEN it is placed, THEN every
   reservation commits immediately and no live reservation remains.

7. **Multi-ticket expiry**

   GIVEN a pending order with reservations across several ticket types expires, WHEN hold expiry is
   processed, THEN every reservation is released, all affected reserved counts are reduced, no
   reservation row remains, and the order becomes expired exactly once.

8. **Fallback consistency**

   GIVEN an order-confirmed or order-expired domain-event handler is invoked through a retry or
   alternate application path, WHEN the order still has live reservations, THEN it uses the complete
   reservation set for that order and cannot leave a later ticket type reserved; confirmed-order
   reconciliation completes before ticket issuance.

9. **No observable contract drift**

   GIVEN a valid existing checkout client submits one line per ticket type, WHEN it places and
   confirms an order, THEN its public HTTP contract and all-inclusive total remain unchanged.

10. **PostgreSQL final-ticket race**

   GIVEN several unauthenticated buyers concurrently request the final free ticket through separate
   HTTP requests, WHEN the requests overlap at the persistence boundary, THEN exactly one order is
   created and confirmed, sold is `1`, reserved is `0`, no live reservation remains, and every other
   request fails with an existing validation/conflict outcome rather than a server error.

## Business Rules and Invariants

- `INV-10`: each ticket type retains `Reserved + Sold <= Capacity`; a confirmed order has no live
  reservations, and an expired order has no live reservations.
- `INV-21`: a pending order's primary reservation pointer remains a compatibility indicator, not a
  complete source of its reservation set. The authoritative association is `Reservation.OrderId` on
  the Event aggregate.
- `INV-24`: per-order limits apply to the sum of all requested quantities for the same ticket type,
  not to an individual transport line.
- `INV-25`: aggregation must occur before snapshot construction; all same-type input lines have the
  current single ticket-type price, so the normalized line total equals their prior sum.
- Order placement remains the documented single transaction that may update Event inventory and
  create an Order. A failed validation must leave both aggregates unchanged.
- Payment notification idempotency remains keyed by the payment/provider reference. A duplicate
  capture must never re-run reservation commitment or ticket issuance.

## Domain and Application Design Notes

- Do not expand `Order.ReservationId` into an incomplete list or add a new persistence relation.
  The Event aggregate already owns the complete reservation collection and its `OrderId` relation.
- Extract a clear local operation in Application, if useful, that enumerates an Event's current
  reservations for the order before mutating the collection. Materialize ids before calling
  `CommitReservation`/`ReleaseReservation`, because each call removes an item from the collection.
- Normalize `PlaceOrderCommand.Lines` by `TicketTypeId` once, then use that normalized collection
  consistently for all lookup, availability, limit, `OrderLine`, and `Reserve` work. Do not perform
  validation on raw lines after calculating aggregate quantities.
- `ConfirmPaymentCommandHandler`, `CommitReservationOnOrderConfirmedHandler`, and
  `ReleaseReservationOnOrderExpiredHandler` must agree on the same complete-set rule. The existing
  expiry command already derives reservations by `OrderId`; preserve that pattern.
- Domain-event fallback handlers must delegate reconciliation to an internal command that uses the
  normal unit-of-work/retry pipeline and dispatches the Event's resulting reservation events only
  after persistence succeeds. They must not save a detached aggregate directly.
- Ticket issuance for an `OrderConfirmedEvent` must request the same idempotent reconciliation
  before it reads or creates tickets. Correctness must not depend on the DI/reflection enumeration
  order of sibling domain-event handlers.
- Expected business-rule failures remain `Result` validation errors. Do not turn stale/incomplete
  reservations into a silent partial confirmation.

## Data and API Expectations

- No new public endpoint, OpenAPI change, migration, or generated frontend type is required.
- Existing public placement input remains an array of lines. Normalizing duplicate transport lines is
  an implementation detail; clients observe valid aggregate quantities and existing error codes.
- The response should continue to expose final total and item quantities accurately. If normalized
  output merges duplicate input, that is the canonical order representation and must be covered by
  integration tests.

## Concurrency and Failure Modes

- Two buyers racing for inventory remain protected by the Event aggregate's optimistic concurrency
  and retry boundary; the repair must not create a second reservation lookup/write outside that
  transaction.
- A provider callback after hold expiry must fail as it does today and must not resurrect released
  inventory.
- A database/optimistic-concurrency failure during confirmation must roll back all reservation and
  order/payment mutations together; it must not commit only a prefix of reservations.
- The `events.row_version` value must advance in the same update that changes Event-owned inventory.
  On a conflict, retry with a cleared change tracker and no pending domain or realtime work retained
  from the failed attempt.
- A duplicate line with aggregate quantity exceeding availability or the limit must fail before any
  reservation is made.
- A mixed free/paid order uses the existing total semantics; the terminal path is chosen from the
  final order total and still commits every reservation exactly once.

## Testing and Verification Strategy

- Add focused API integration tests beside `PlaceOrderMaxPerOrderTests`, `PaymentTests`, and
  `HoldExpiryTests` using two ticket types and fictional contacts.
- Cover: duplicate-line limit rejection; allowed duplicate-line normalization; successful paid
  multi-type confirmation; duplicate paid callback; free multi-type confirmation; and multi-type
  hold expiry. Assert both reservation rows and every ticket type's `Reserved`/`Sold` state.
- Add a real PostgreSQL concurrent final-ticket test. It must prove one created order, one sold unit,
  no live reservation, and safe non-success responses for all losing requests.
- Directly dispatch confirmed and expired order events against intentionally residual multi-ticket
  reservations to prove each fallback routes through the same complete-set reconciliation behavior.
- Retain current single-type tests as regression coverage; do not replace them with only multi-type
  tests.
- Run the focused integration test filters, then the full API integration project and full .NET
  suite. Run the changed-code verifier before handoff.
- Web and Playwright work are explicitly excluded: the public API schema and normal single-line UI
  behavior do not change, while integration tests directly prove the transaction and persistence
  invariants.

## Risks and Follow-up Decisions

- Existing implementation specs for F-3.4/F-3.6/F-5.3 describe multi-line aggregation and multiple
  reservations but current code contradicts them. This repair spec is current implementation
  evidence; after acceptance, reconcile the stale completion assertions in those adjacent specs if
  needed without creating competing source-of-truth behavior.
- Existing production data may contain confirmed paid orders with non-primary live reservations from
  the prior defect. Before deployment, an operator must audit those rows and run an approved,
  rollback-safe reconciliation that commits or releases each stale reservation according to the
  authoritative order/payment state. This repair does not silently mutate historical production data.
- The `Order.ReservationId` compatibility field remains conceptually misleading. Removing it would
  require a separately scoped schema/API/domain migration and is not necessary to correct the
  invariant today.
- The later paid-checkout security repair must preserve this complete-set reservation logic when it
  changes webhook trust handling.
- The current MediatR registration order writes pending session-cache state before the retrying
  unit-of-work has committed. Correcting that pipeline ordering is a session-handling behavior
  change requiring explicit human approval; it is excluded from this checkout repair and must be
  resolved before relying on session-cache state after a retried session mutation.

## Implementation Planning Notes

- Risk tier: High. Inventory and money-confirmation paths are affected even though no public HTTP
  shape changes.
- Required areas: Domain/Application/Infrastructure-backed API integration tests. Contracts, web,
  and E2E are source-backed exclusions for this repair.
- Mandatory gates: code review and acceptance verification; security review is not required unless
  implementation expands into provider authentication or sensitive-data behavior.
