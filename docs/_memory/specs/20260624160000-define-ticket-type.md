---
artifact_type: spec
artifact_version: 1
id: spec-20260624160000-define-ticket-type
title: Define a ticket type
slug: define-ticket-type
filename_template: 20260624160000-define-ticket-type.md
created_at: "2026-06-24T16:00:00Z"
updated_at: "2026-06-24T16:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, ticketing]
feature_refs: ["F-3.1"]
ddd_refs: ["BC-2", "AGG-Event", "ENT-TicketType", "INV-10", "INV-12", "INV-13", "VO-Money", "VO-Capacity"]
prd_refs: ["DEC-1", "DEC-3", "QG-1", "QG-2", "QG-5"]
tech_refs: ["Tech §4", "Tech §6", "Tech §7"]
db_refs: ["Tech §6"]
github_issue: 34
search_index:
  keywords: [ticket-type, price, capacity, inventory, free-ticket, event, organizer, name, quantity]
  bounded_contexts: ["Event Management"]
  user_personas: ["PER-O1", "PER-O2"]
> GitHub: #34 (https://github.com/tranvuongduy2003/eventhub/issues/34)

---

# Feature: Define a ticket type

> Features: F-3.1  |  Status: DRAFT  |  Date: 2026-06-24
> PRD: DEC-1 (no platform fee), DEC-3 (MVP scope), QG-1 (simplicity), QG-2 (transparent pricing), QG-5 (no oversell)  |  DDD: BC-2 AGG-Event, ENT-TicketType, INV-10, INV-12, INV-13  |  Tech: §4, §6, §7

## 1. Problem & Solution

**Problem:** An organizer has created a draft event (F-2.1), but there is nothing to sell yet. Without a ticket type — a named, priced, quantity-limited category of admission — the event cannot be published (F-2.4 requires at least one) and attendees have nothing to purchase.

**Solution:** Let the organizer attach one or more ticket types to a draft event. Each ticket type has a name (e.g., "General Admission", "VIP"), a price in the configured currency (zero for free), and a capacity (maximum number that may be sold). Ticket types are the foundation for inventory tracking (F-3.4), transparent pricing (F-3.3), and the entire purchase flow (EP-5).

**Personas:** PER-O1 (individual organizer) and PER-O2 (small group organizer) — the people who define what can be sold at their events.

**Scope:** F-3.1 only. Free tickets (F-3.2), transparent pricing display (F-3.3), inventory and no-oversell (F-3.4), multiple ticket types (F-3.5), and per-order limits (F-3.6) are specified separately. This feature covers the creation of a single ticket type on a draft event.

## 2. Acceptance Criteria

**AC-01:** GIVEN I hold the Owner role for a Draft event (F-1.5), WHEN I add a ticket type with a name, a price (in the configured currency, non-negative), and a capacity (positive integer), THEN the ticket type is created and attached to the event.

**AC-02:** GIVEN I hold the Owner role for a Draft event, WHEN I add a ticket type with a price of zero, THEN it is created as a free ticket type (F-3.2 handles checkout behavior for free tickets).

**AC-03:** GIVEN I do not hold the Owner role for the event, WHEN I attempt to add a ticket type, THEN I am refused with an "insufficient permissions" message and no ticket type is created.

**AC-04:** GIVEN I hold the Owner role, WHEN I attempt to add a ticket type with a negative price, THEN the action is rejected with a clear message that price must be non-negative.

**AC-05:** GIVEN I hold the Owner role, WHEN I attempt to add a ticket type with a capacity of zero or a negative number, THEN the action is rejected with a clear message that capacity must be a positive integer.

**AC-06:** GIVEN I hold the Owner role, WHEN I attempt to add a ticket type with an empty or blank name, THEN the action is rejected with a clear message that a name is required.

**AC-07:** GIVEN I hold the Owner role for a Published, Closed, or Cancelled event, WHEN I attempt to add a ticket type, THEN the action is rejected with a message indicating the event's current status does not allow adding ticket types (editing ticket types on live events is a future enhancement via F-2.3 / F-3.5).

**AC-08:** GIVEN I hold the Owner role for a Draft event with no ticket types, WHEN I attempt to publish the event (F-2.4), THEN publishing is blocked with a message that at least one ticket type is required.

**AC-09:** GIVEN I hold the Owner role, WHEN I add a ticket type, THEN I can see it listed on the event with its name, price, and capacity.

## 3. Domain & Business Rules

Referenced from `domain-model-specification.md`:

- **ENT-TicketType:** An entity within the Event aggregate. Each ticket type has a unique identity (`VO-TicketTypeId`), a name (`VO-TicketName`), a price (`VO-Money`), a capacity (`VO-Capacity`), and counters for sold and reserved quantities (initially zero).
- **INV-10 — No-oversell:** `Reserved + Sold ≤ Capacity` per ticket type. This invariant is enforced at the aggregate level and becomes active once inventory operations (reserve, commit, release) exist in F-3.4. For F-3.1, capacity is simply stored as the ceiling.
- **INV-12 — Capacity guard:** Capacity cannot be reduced below `Reserved + Sold`. For F-3.1 (new ticket type, zero sold/reserved), this means capacity must be at least 1 (positive integer).
- **INV-13 — Price ≥ 0:** A ticket type's price must be non-negative. Zero is a valid price representing a free ticket.
- **Ownership enforcement:** Only the user holding the Owner role (F-1.5) for the specific event may add ticket types. This is an application-layer authorization check, not a domain invariant.
- **Aggregate boundary:** Ticket types are entities inside the Event aggregate, not separate aggregates. Adding a ticket type mutates the Event aggregate and is subject to optimistic concurrency.

## 4. API Contract

**Endpoint:** `POST /api/events/{eventId}/ticket-types`

**Auth:** Cookie session required. The handler verifies the caller holds the Owner role for the event.

**Request body:**

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| name | string | yes | Non-empty, trimmed |
| price | object | yes | `{ amount: number, currency: string }` — amount ≥ 0; currency matches the configured single currency |
| capacity | integer | yes | ≥ 1 |

**Success (201):** Returns the created ticket type with its generated identity, name, price, capacity, and zero sold/reserved counts.

**Error responses:**

| Status | Code | Condition |
|--------|------|-----------|
| 401 | `UNAUTHORIZED` | No active session |
| 403 | `FORBIDDEN` | Caller does not hold the Owner role for this event |
| 404 | `NOT_FOUND` | Event does not exist |
| 422 | `INVALID_TICKET_TYPE_NAME` | Name is empty or blank |
| 422 | `INVALID_TICKET_TYPE_PRICE` | Price amount is negative |
| 422 | `INVALID_TICKET_TYPE_CAPACITY` | Capacity is zero or negative |
| 422 | `INVALID_EVENT_STATUS` | Event is Published, Closed, or Cancelled |

## 5. Data & Storage Impact

- **PostgreSQL:** A new row is inserted in the ticket types table (within the `app` schema), linked to the event via a foreign key. Fields: `id`, `event_id`, `name`, `price_amount`, `price_currency`, `capacity`, `sold` (default 0), `reserved` (default 0). The event's `row_version` optimistic concurrency token is incremented.
- **Redis:** No direct cache impact. Ticket type data becomes visible through the event query path, which may cache independently.
- **MinIO:** No change — no binary assets involved in ticket type creation.
- **RabbitMQ:** No integration events emitted for ticket type creation at this stage. `EVT-TicketTypeAdded` is a domain event handled in-process if needed by future features.

## 6. Real-Time & Consistency

- **Strong consistency:** Adding a ticket type is a single-transaction mutation on the Event aggregate. Optimistic concurrency prevents conflicting edits (e.g., two users adding ticket types to the same event simultaneously — one will get a 409 conflict and must retry).
- **No integration events:** Ticket type creation does not cross bounded context boundaries. No RabbitMQ message is emitted.
- **N/A for SignalR (MVP):** Real-time push for ticket type changes is not required. It becomes relevant in EP-11 (live sales monitoring).

## 7. Security & Privacy

- **Session auth required:** The caller must be signed in. No guest or anonymous ticket type creation.
- **Owner-only authorization:** Checked in the Application handler via `ICurrentUserAccessor` — not just at the API endpoint level.
- **No sensitive data exposed:** Ticket type data (name, price, capacity) is non-sensitive. It will become public when the event is published (F-4.1).
- **Currency validation:** The price currency must match the system's single configured currency. No multi-currency support (out of scope per `product-requirements.md` §6.2).

## 8. Edge Cases

**EC-01:** Two Owner-level users attempt to add ticket types to the same Draft event simultaneously. The system processes one and returns a 409 conflict to the other due to optimistic concurrency on the Event aggregate. The rejected user retries.

**EC-02:** An organizer adds a ticket type with a very large capacity (e.g., 999,999). The system accepts it — capacity is a positive integer with no upper bound at this stage. Capacity constraints are the organizer's responsibility for the MVP.

**EC-03:** An organizer adds a ticket type with a name that duplicates an existing ticket type on the same event. The system allows it — duplicate names are not rejected. The organizer is responsible for meaningful naming. (A uniqueness constraint on name-per-event could be a future enhancement.)

**EC-04:** An organizer adds a ticket type, then the event is published, then the organizer tries to add another ticket type. The system rejects the addition because the event is no longer Draft. Adding ticket types to live events is deferred to F-3.5.

**EC-05:** The price currency in the request does not match the system's configured currency. The system rejects the request with a clear message about the supported currency.

**EC-06:** An organizer provides a price with more decimal places than the currency supports (e.g., 10.999 for a currency with 2 decimal places). The system rounds or rejects based on the configured currency's precision.

## 9. Dependencies & Risks

**Dependencies:**
- F-2.1 (Create a draft event) — the event must exist before ticket types can be added
- F-1.5 / F-1.6 (Roles) — Owner role must be assignable and checkable

**Risks:**
- Coupling ticket types to the Event aggregate means adding a ticket type increments the event's row version. If the organizer is editing event details (F-2.3) and adding ticket types concurrently, they may hit optimistic concurrency conflicts. This is acceptable at MVP scale.
- No validation that a ticket type name is meaningful or non-duplicate. This is a deliberate simplicity choice (QG-1) and can be tightened later.

## 10. Assumptions

- The system uses a single configured currency (no multi-currency). The currency is set at the system level, not per-event or per-ticket-type.
- Ticket type names are free-text strings with no predefined categories (e.g., "General Admission", "VIP", "Early Bird" are all just names).
- Capacity is a hard ceiling, not a suggestion. It is the maximum number of tickets that can be sold for this type.
- Adding a ticket type to a Draft event does not trigger any notifications or integration events.
- The ticket type identity is a system-generated UUID, not organizer-editable.
- Ticket types can be edited or deleted on Draft events (covered by F-2.3 / F-3.5). This spec covers creation only.

## 11. Out of Scope

- **Editing ticket types after creation:** Covered by F-2.3 (Edit event details) and F-3.5 (Multiple ticket types per event).
- **Deleting ticket types:** Not in this feature slice. May be part of F-2.3 or F-3.5.
- **Multiple ticket types per event (F-3.5):** This spec covers adding a single ticket type. The organizer can add multiple types, but F-3.5 specifies the richer behavior (tiers, independent tracking, display).
- **Per-order purchase limits (F-3.6):** A future enhancement that caps how many tickets one order may buy.
- **Discount codes (F-3.7):** A future enhancement for percentage or fixed-amount reductions.
- **Scheduled on-sale windows (F-3.8):** A future enhancement for time-limited sales.
- **Inventory tracking and no-oversell guarantee (F-3.4):** The capacity field is stored here, but the reservation/sold tracking and the no-oversell invariant are specified in F-3.4.
- **Transparent pricing display (F-3.3):** The price is stored here, but how it is shown to attendees on the public page and checkout is specified in F-3.3.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should ticket type names be unique within an event, or are duplicates allowed? **Decision:** Duplicates are allowed for MVP simplicity (QG-1). The organizer is responsible for meaningful naming. | ✅ |
| 2 | Should there be an upper bound on capacity? **Decision:** No upper bound for MVP. The organizer sets whatever capacity makes sense for their venue. | ✅ |
| 3 | Should adding a ticket type to a Draft event be restricted to Draft only, or also allowed on Published events? **Decision:** Draft only for this feature slice. Adding ticket types to live events is deferred to F-3.5, which handles the additional complexity (inventory impact, display updates). | ✅ |
