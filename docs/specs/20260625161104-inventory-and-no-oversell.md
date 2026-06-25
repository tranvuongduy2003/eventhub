---
artifact_type: spec
artifact_version: 1
id: spec-20260625161104-inventory-and-no-oversell
title: Inventory and No-Oversell Guarantee
slug: inventory-and-no-oversell
filename_template: 20260625161104-inventory-and-no-oversell.md
created_at: 2026-06-25T16:11:04Z
updated_at: 2026-06-25T16:11:04Z
status: draft
owner: product
tags: [spec, eventhub, ticketing-transparent-pricing]
feature_refs: [F-3.4]
ddd_refs: [BC-2, AGG-Event, ENT-TicketType, ENT-Reservation, INV-10, INV-12, INV-14]
prd_refs: [DEC-1, QG-1, QG-5, RSK-5, PRD 3.4]
tech_refs: [Tech 4, Tech 5, Tech 6, Tech 7]
db_refs: [Tech 6]
github_issue: 40
search_index:
  keywords: [inventory, oversell, reservation, capacity, sold-out, concurrency, optimistic-concurrency, availability, hold, ticket]
  bounded_contexts: [BC-2, BC-3]
  user_personas: [PER-O1, PER-A1]
---

> GitHub: #40 (https://github.com/tranvuongduy2003/rebuilt-eventhub/issues/40)

# Feature: Inventory and No-Oversell Guarantee

> Features: F-3.4  |  Status: DRAFT  |  Date: 2026-06-25
> PRD: QG-5, RSK-5, 3.4  |  DDD: INV-10, INV-12, INV-14, AGG-Event  |  Tech: 4, 6

## 1. Problem & Solution

**Problem:** Without inventory tracking, the platform could sell more tickets than a venue can hold, leading to oversold events, frustrated attendees, and organizer trust erosion. Simultaneous buyers targeting the last ticket create a race condition that must resolve to exactly one winner.

**Solution:** Track per-ticket-type availability (`Available = Capacity - Reserved - Sold`) on the Event aggregate. Use optimistic concurrency with retry so that two buyers racing for the last ticket — exactly one succeeds and the other receives a sold-out rejection. Reservations are time-limited holds; they either commit to sales or release back to the pool.

**Personas:**
- **PER-O1 (Organizer):** Needs confidence that inventory numbers are accurate and that overbooking is impossible.
- **PER-A1 (Attendee):** Expects that if a ticket is shown as available, purchasing it will succeed — and that sold-out is clearly communicated.

**Scope:**
- **In:** Reserve inventory, commit reservation (reserved → sold), release reservation (hold expiry), return to pool (ticket return), sold-out detection and rejection, availability calculation, optimistic concurrency for race conditions.
- **Out:** Ticket return flow (F-10.3 — only the inventory-return mechanism is in scope), live inventory dashboards (F-11.1), sold-out nudges (F-11.3), low-stock indicators, multi-currency, secondary marketplace.

## 2. Acceptance Criteria

**AC-01:** GIVEN a ticket type with capacity 100 and 0 sold and 0 reserved, WHEN availability is queried, THEN available equals 100.

**AC-02:** GIVEN a ticket type with capacity 100, 40 sold, and 30 reserved, WHEN availability is queried, THEN available equals 30.

**AC-03:** GIVEN a ticket type with capacity 100 and 98 sold and 2 reserved (available = 0), WHEN a new attendee tries to reserve 1 ticket, THEN the reservation is rejected with a sold-out error and the ticket type is marked as sold out.

**AC-04:** GIVEN a ticket type with capacity 1 and 0 sold and 0 reserved (available = 1), WHEN two attendees simultaneously attempt to reserve that last ticket, THEN exactly one reservation succeeds and the other receives a sold-out rejection.

**AC-05:** GIVEN a successful reservation of 3 tickets, WHEN the reservation is committed (order confirmed), THEN the 3 tickets move from Reserved to Sold and the available count decreases accordingly.

**AC-06:** GIVEN a reservation of 2 tickets, WHEN the hold expires without payment, THEN the 2 tickets move from Reserved back to the available pool (Reserved decrements) and the attendee's order is expired.

**AC-07:** GIVEN a ticket type that is not published (draft or closed event), WHEN an attendee attempts to reserve tickets, THEN the reservation is rejected with an appropriate error.

**AC-08:** GIVEN a ticket type with a defined sales window, WHEN an attendee attempts to reserve tickets outside that window (before opening or after closing), THEN the reservation is rejected.

**AC-09:** GIVEN a reservation attempt for 5 tickets on a ticket type with only 3 available, WHEN the reservation is processed, THEN it is rejected because the requested quantity exceeds availability.

**AC-10:** GIVEN a ticket type with capacity 100, WHEN the capacity is reduced by the organizer, THEN the new capacity must be greater than or equal to Reserved + Sold (the reduction is rejected if it would violate this constraint).

**AC-11:** GIVEN inventory operations (reserve, commit, release, return), WHEN any operation completes, THEN domain events are raised for downstream consumers (inventory reserved, reservation committed, reservation released, inventory returned to pool).

**AC-12:** GIVEN a ticket type whose availability just reached 0, WHEN the sold-out condition is detected, THEN an integration event is emitted so realtime dashboards and notification systems can react.

## 3. Domain & Business Rules

### Invariants (from ddd.md)

| ID | Rule | Enforcement |
|----|------|-------------|
| **INV-10** | `Reserved + Sold <= Capacity` per ticket type | Aggregate-level check on every Reserve, CommitReservation, ReleaseReservation, ReturnToPool, and capacity change |
| **INV-12** | Capacity cannot be reduced below `Reserved + Sold` | Aggregate-level check on capacity update |
| **INV-14** | Cannot reserve unless the event is Published, within SalesWindow (if defined), and not Closed/Cancelled | Aggregate-level precondition check on Reserve |

### Inventory Lifecycle

1. **Reserve** — Creates a time-limited hold. Increments `Reserved` on the ticket type. Validates INV-10 and INV-14. If availability reaches 0 after reservation, the ticket type is effectively sold out for subsequent requests.
2. **CommitReservation** — Converts reserved quantity to sold. Decrements `Reserved`, increments `Sold`. Triggered when an order is confirmed (payment captured for paid, auto-confirmed for free).
3. **ReleaseReservation** — Returns reserved quantity to the available pool. Decrements `Reserved`. Triggered when a hold expires without payment.
4. **ReturnToPool** — Returns sold quantity to the available pool. Decrements `Sold`. Triggered when a ticket is returned post-purchase (F-10.3, out of scope for this spec's implementation but the mechanism is provided).

### Domain Events

| Event | Type | Trigger | Consumers |
|-------|------|---------|-----------|
| InventoryReserved | Domain | Reservation created | Same unit of work |
| ReservationCommitted | Domain | Reserved → Sold transition | Same unit of work |
| ReservationReleased | Integration | Hold expired / released | Sales (expire order) |
| InventoryReturnedToPool | Domain/Integration | Ticket returned to pool | Sales, Reporting |
| EventSoldOut | Integration | Availability hits 0 | Realtime (F-11.1), Nudges (F-11.3) |

### Concurrency Strategy

The no-oversell guarantee (INV-10) is enforced via **optimistic concurrency** on the Event aggregate. The Event aggregate carries a `row_version` column. When two concurrent requests attempt to reserve the last ticket:

1. Both read the same current state (available = 1).
2. Both attempt to reserve — one succeeds and commits with an incremented row version.
3. The other's commit fails the row version check. `UnitOfWorkBehavior` detects the concurrency conflict and retries the command.
4. On retry, the command re-reads the aggregate, sees available = 0, and the domain logic rejects the reservation with a sold-out error.

This is the documented "pragmatic two-aggregate write" — `PlaceOrder` reserves on Event and creates an Order in a single transaction for strong consistency.

## 4. UI Behavior or API Contract

### API Endpoints

Inventory operations are internal domain behaviors invoked by command handlers. They are not directly exposed as standalone API endpoints. The following endpoints consume inventory indirectly:

| Endpoint | Action | Inventory Effect |
|----------|--------|-----------------|
| `POST /api/orders` (PlaceOrder) | Start checkout | Reserve inventory (F-5.1, F-5.3) |
| Order confirmation flow | Payment captured or free auto-confirm | CommitReservation |
| Hold expiry background job | Timeout without payment | ReleaseReservation |
| `GET /api/events/{eventId}/public` | View event | Display availability and sold-out status |

### Availability Display

The public event page (`GET /api/events/{eventId}/public`) must include per-ticket-type availability information:

- **Available count** (or "Available" when > 0)
- **"Sold Out"** badge when available = 0
- **"Low Stock"** indicator when available is below a threshold (optional, for F-11.3 — out of scope here but the data is provided)

### Error Responses

| Scenario | Error Code | HTTP Status |
|----------|-----------|-------------|
| Ticket type sold out | `TICKET_TYPE_SOLD_OUT` | 422 |
| Requested quantity exceeds availability | `INSUFFICIENT_AVAILABILITY` | 422 |
| Event not published | `EVENT_NOT_PUBLISHED` | 422 |
| Outside sales window | `SALES_WINDOW_CLOSED` | 422 |
| Concurrency conflict (after retry exhaustion) | `CONCURRENCY_CONFLICT` | 409 |

## 5. Data & Storage Impact

### PostgreSQL (app schema)

**No new tables required.** Inventory state lives on the existing `TicketType` entity within the Event aggregate:

| Column | Type | Purpose |
|--------|------|---------|
| `capacity` | integer | Maximum quantity (set by organizer, constrained by INV-12) |
| `reserved` | integer | Currently held by in-flight orders |
| `sold` | integer | Confirmed sales |

**Reservation entity** (new, owned by Event aggregate):

| Column | Type | Purpose |
|--------|------|---------|
| `id` | uuid | Primary key |
| `ticket_type_id` | uuid | FK to ticket type |
| `quantity` | integer | Number of tickets held |
| `order_id` | uuid | Back-reference to the order |
| `expires_at` | timestamptz | Hold deadline |
| `created_at` | timestamptz | Audit |

The Reservation entity is an owned entity within the Event aggregate — it lives in the same transaction boundary. The `row_version` column on the Event aggregate root guards all inventory mutations via optimistic concurrency.

### Redis

No direct Redis impact. Availability is computed from PostgreSQL state. Read-side caching of availability (if any) is rebuildable and out of scope for this feature.

### RabbitMQ

Integration events (`ReservationReleased`, `EventSoldOut`) are published after successful commit. Consumers are idempotent.

## 6. Real-Time & Consistency

### Consistency Model

- **Strong consistency** inside the Event aggregate — all inventory mutations (reserve, commit, release, return) are transactional with optimistic concurrency retry.
- **Pragmatic two-aggregate write** — `PlaceOrder` mutates both Event (reserve) and Order (create) in one transaction. This is the documented exception to one-aggregate-per-transaction, justified by the no-oversell hard requirement.
- **Eventual consistency** for cross-context notifications — `ReservationReleased` and `EventSoldOut` are integration events delivered via RabbitMQ after commit. Consumers are idempotent.

### Realtime (F-11.1, F-11.3 — out of scope but data provided)

The `EventSoldOut` integration event is the hook for realtime dashboards and sold-out nudges. This spec emits the event; consuming it is F-11.1/F-11.3's responsibility.

## 7. Security & Privacy

- **Authorization:** Only authenticated attendees with valid sessions can place orders (reserve inventory). Guest checkout (if enabled per F-5.2) uses the same reservation path with a guest session.
- **Ownership:** CommitReservation and ReleaseReservation are triggered by system processes (payment confirmation, hold expiry) — not directly by user action. The handler verifies the order belongs to the caller before committing.
- **No sensitive data:** Inventory counts (capacity, reserved, sold, available) are public information displayed on event pages.
- **Rate limiting:** Not in scope for this feature but recommended as a platform concern.

## 8. Edge Cases

**EC-01: Simultaneous last-ticket purchase.** Two buyers race for the last ticket. Optimistic concurrency ensures exactly one wins; the other gets a sold-out error after retry. See AC-04 and §3 Concurrency Strategy.

**EC-02: Reserve exactly available quantity.** A buyer requests N tickets and N are available. The reservation succeeds, available drops to 0, and subsequent requests are rejected as sold out.

**EC-03: Hold expiry during high demand.** A reservation expires while other buyers are waiting. The released quantity becomes available; the next reservation attempt can succeed.

**EC-04: Organizer reduces capacity below sold+reserved.** The operation is rejected (INV-12). The organizer must wait for holds to expire or cancel orders first.

**EC-05: Free ticket type inventory.** Free tickets consume inventory identically to paid tickets. A free event can sell out. Two attendees racing for the last free ticket follow the same no-oversell guarantee.

**EC-06: Reservation for more than available.** A buyer requests 5 tickets but only 3 are available. The reservation is rejected with `INSUFFICIENT_AVAILABILITY` — no partial reservation.

**EC-07: Event state change during active reservations.** If an event is unpublished or cancelled while reservations exist, existing reservations proceed to commit or expiry. New reservations are blocked by INV-14.

**EC-08: Retry exhaustion.** Under extreme contention, if optimistic concurrency retries are exhausted, the command fails with `CONCURRENCY_CONFLICT` (409). The client may retry the entire operation.

## 9. Dependencies & Risks

### Dependencies

| Feature | Relationship |
|---------|-------------|
| F-3.1 (Define ticket type) | **Complete.** TicketType entity with capacity, sold, reserved fields exists. |
| F-3.2 (Free tickets) | **Complete.** Order aggregate and PlaceOrder exist. Inventory applies identically to free tickets. |
| F-3.3 (Transparent pricing) | **Complete.** Price snapshotting ensures order totals are immutable; no interaction with inventory. |
| EP-2 (Event management) | Event aggregate and lifecycle exist. |

### Downstream Features (unblocked by F-3.4)

| Feature | What it needs from F-3.4 |
|---------|--------------------------|
| F-5.1 (Select tickets, start checkout) | Reserve inventory during checkout |
| F-5.3 (Create order, hold inventory) | Reservation + hold timer |
| F-10.3 (Return ticket to pool) | ReturnToPool mechanism |
| F-11.1 (Live sales, inventory) | Real-time availability data |
| F-11.3 (Sold-out nudges) | EventSoldOut integration event |

### Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Hot-aggregate contention under high concurrency | Low (small events) | Optimistic concurrency + 3 retries; known scaling path (split inventory aggregate) is out of scope |
| Hold expiry timing — too short causes false sold-outs, too long ties up inventory | Medium | Configurable per ticket type by organizer; 15 minutes default |
| Partial failure between reserve and order creation | Low | Single transaction (pragmatic two-aggregate write); either both succeed or both roll back |

## 10. Assumptions

1. **Small-event scale.** Per QG-1 and QG-5, the platform targets small events. Optimistic concurrency is sufficient; distributed locking or inventory sharding is not needed.
2. **Single database.** PostgreSQL is the sole authoritative store. No cross-database transactions.
3. **Existing aggregate boundaries.** Inventory lives on the Event aggregate (not a separate Inventory aggregate). This is the documented design in ddd.md.
4. **Reservation hold duration.** Default is 15 minutes. The organizer can configure hold duration per ticket type when setting up or editing a ticket type.
5. **No partial reservations.** A reservation request is all-or-nothing — if the requested quantity exceeds availability, the entire request is rejected.
6. **Row version on Event.** The Event aggregate already carries a `row_version` column (from F-3.1 / EP-2). If not, it must be added as part of this feature.

## 11. Out of Scope

- Ticket return flow (F-10.3) — only the `ReturnToPool` domain mechanism is provided; the full return UX and policy are F-10.3's scope.
- Live inventory dashboards (F-11.1) — the `EventSoldOut` integration event is emitted but consumption is F-11.1.
- Sold-out / low-stock nudges (F-11.3) — same as above.
- Inventory splitting into a separate aggregate (known scaling option).
- Distributed locking or external concurrency control.
- Multi-currency inventory.
- Waitlist or back-in-stock notifications.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | What is the default reservation hold duration? | ✅ 15 minutes default |
| 2 | Should the organizer be able to configure hold duration per ticket type, or is a platform-wide default sufficient for MVP? | ✅ Configurable per ticket type by the organizer |
| 3 | What is the maximum retry count for optimistic concurrency conflicts before returning 409? | ✅ 3 retries |
