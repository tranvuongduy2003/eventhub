---
artifact_type: spec
artifact_version: 1
id: spec-20260626000000-multiple-ticket-types-per-event
title: Multiple ticket types per event
slug: multiple-ticket-types-per-event
filename_template: 20260626000000-multiple-ticket-types-per-event.md
created_at: "2026-06-26T00:00:00Z"
updated_at: "2026-06-26T00:00:00Z"
status: draft
plan_ready: true
owner: product
tags: [spec, eventhub, ticketing]
feature_refs: [F-3.5, F-3.1, F-3.3, F-3.4, F-4.1, F-5.1]
ddd_refs: [BC-2, AGG-Event, ENT-TicketType, INV-10, INV-12, INV-13]
prd_refs: [DEC-1, QG-1, QG-2, QG-3, QG-5]
tech_refs: [Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 2
search_index:
  keywords: [ticket types, tiers, VIP, early-bird, general admission, pricing, capacity, inventory, event page, checkout]
  bounded_contexts: [Event Management, Sales]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #2 (https://github.com/tranvuongduy2003/EventHub/issues/2)

# Feature: Multiple ticket types per event

> Features: F-3.5  |  Status: DRAFT  |  Date: 2026-06-26
> PRD: DEC-1 (no platform fee), QG-1 (simplicity), QG-2 (transparent pricing), QG-5 (correct at small scale)
> DDD: BC-2 Event Management, AGG-Event, ENT-TicketType, INV-10 (no-oversell)

## 1. Problem & Solution

**Problem:** An event often needs more than one way to admit people — a general admission tier, a VIP tier, an early-bird discount tier, or a student rate. With only one ticket type per event, organizers must create separate events or work around the limitation, which is confusing for attendees and unmanageable for organizers.

**Solution:** Allow an organizer to define multiple ticket types on a single event, each with its own name, price, and capacity. Each type is independently tracked for inventory and sales. On the public event page and during checkout, all types are presented together so the attendee can choose the tier that suits them.

**Personas:** PER-O1 (individual organizer) — wants simple multi-tier setup. PER-O2 (small group/club) — needs structured tiers like VIP vs General for pricing differentiation.

**Scope:**
- **In:** Adding, editing, and removing multiple ticket types on a single event; independent inventory per type; all types visible on the public event page; all types selectable during checkout; per-type sold-out state.
- **Out:** Discount codes (F-3.7), per-order purchase limits (F-3.6), scheduled on-sale windows (F-3.8) — these are separate features that build on the multi-type foundation.

## 2. Acceptance Criteria

**AC-01:** GIVEN I hold the Owner role for the event (F-1.5) AND the event has one ticket type already, WHEN I add a second ticket type with a distinct name, a price (≥ 0), and a capacity, THEN the new ticket type is attached to the event alongside the existing one.

**AC-02:** GIVEN an event with multiple ticket types, WHEN I view the event, THEN each ticket type is listed with its own name, price, and availability status (available count or "sold out").

**AC-03:** GIVEN I hold the Owner role for the event, WHEN I edit a specific ticket type's name, price, or capacity, THEN only that type changes; the other types remain unaffected.

**AC-04:** GIVEN I hold the Owner role for the event, WHEN I attempt to reduce a ticket type's capacity below the number already reserved plus sold, THEN the change is blocked with a clear explanation (INV-12).

**AC-05:** GIVEN an event with multiple ticket types, WHEN an attendee opens the public event page (F-4.1), THEN all ticket types are displayed with their final all-inclusive prices (F-3.3), and each type's availability is shown.

**AC-06:** GIVEN an event with multiple ticket types, WHEN an attendee starts checkout (F-5.1), THEN they can select quantities from one or more types independently, subject to each type's availability.

**AC-07:** GIVEN two ticket types on the same event (Type A with 5 remaining, Type B sold out), WHEN an attendee attempts to add Type B to their order, THEN they are told Type B is sold out, and they can still proceed with Type A.

**AC-08:** GIVEN an event with multiple ticket types, when two attendees race for the last ticket of a specific type, then exactly one succeeds and the other is told that type sold out (INV-10 per type).

**AC-09:** GIVEN I do not hold the Owner role for the event, WHEN I attempt to add, edit, or remove a ticket type, THEN I am refused with an "insufficient permissions" message.

**AC-10:** GIVEN I hold the Owner role, WHEN I remove a ticket type that has zero sold and zero reserved, THEN the type is removed from the event. If the type has sold or reserved tickets, removal is blocked with an explanation.

**AC-11:** GIVEN an event already has 10 ticket types, WHEN I attempt to add another, THEN the addition is blocked with a clear message indicating the maximum of 10 types has been reached.

## 3. Domain & Business Rules

**Bounded context:** BC-2 — Event Management. Ticket types are entities within the Event aggregate (ENT-TicketType).

**Invariants (from ddd.md):**
- **INV-10:** `Reserved + Sold ≤ Capacity` per ticket type — the no-oversell guarantee applies independently to each type.
- **INV-12:** Capacity cannot be reduced below `Reserved + Sold` for that type.
- **INV-13:** Price ≥ 0 per ticket type (zero = free ticket per F-3.2).
- **INV-11:** An event must have at least one ticket type to be published (F-2.4). Removing the last type from a published event is not allowed.

**Constraints:**
- Maximum of 10 ticket types per event.

**Lifecycle rules:**
- Each ticket type tracks its own `Sold` and `Reserved` counts independently.
- When a ticket type's `Available` (= Capacity − Reserved − Sold) reaches zero, that specific type is marked **sold out**; other types on the same event continue selling normally.
- Price changes on a ticket type do not affect already-placed orders (INV-25 — price snapshot at order placement).

**Events (domain/integration):**
- `EVT-TicketTypeAdded` — raised when a new type is added to an event.
- `EVT-EventSoldOut` — raised per type when availability hits zero. If all types are sold out, the event itself is effectively sold out.

## 4. UI Behavior

**Organizer — Event edit screen:**
- The ticket types section shows a list of all types defined on the event.
- Each type displays: name, price, capacity, sold count, reserved count, and availability.
- An "Add ticket type" action creates a new entry in the list (disabled when 10 types exist, with a tooltip explaining the limit).
- Each type has inline edit (name, price, capacity) and a remove action (guarded by AC-10).
- Validation messages appear inline: capacity below sold+reserved, duplicate name, negative price.

**Public event page (EP-4):**
- All ticket types are rendered as a list or card set, each showing: name, final all-inclusive price, and availability status ("X remaining" or "Sold out").
- Sold-out types are visually distinct (e.g., greyed out, "Sold out" badge) but still visible so attendees can see what was offered.

**Checkout (EP-5):**
- The ticket selection step shows all available types with quantity selectors.
- Sold-out types are not selectable but are shown as sold out.
- Each selected type becomes a separate line item in the order summary with its own unit price and quantity.

## 5. Data & Storage Impact

**PostgreSQL (app schema):**
- The `ticket_types` table (child of `events`) already exists from F-3.1. Each row represents one type with its own `name`, `price`, `capacity`, `sold`, `reserved` columns.
- No schema change required — the table already supports multiple rows per event.
- An index on `(event_id, name)` for uniqueness of type names within an event is recommended if not already present.

**Redis:** No direct impact. Availability counts live in PostgreSQL (authoritative) and are read on demand.

**MinIO:** No impact.

**RabbitMQ:** `EVT-TicketTypeAdded` and per-type `EVT-EventSoldOut` are published as integration events for downstream consumers (Reporting, Realtime).

## 6. Real-Time & Consistency

**Consistency:**
- Inventory per ticket type is strongly consistent within the Event aggregate (optimistic concurrency + retry, INV-10). The existing mechanism from F-3.4 applies per type without modification.
- Cross-context consistency (Sales, Ticketing, Reporting) remains eventual via integration events.

**Real-time (EP-11 — future):**
- When live sales monitoring (F-11.1) is built, it should show sold/remaining per type, not just per event. This spec does not implement the realtime push; it ensures the data model supports it.

## 7. Security & Privacy

- **Authorization:** All ticket type mutations (add, edit, remove) require the Owner role for the event (F-1.5). Staff users cannot modify ticket types.
- **Public read:** Ticket type names, prices, and availability are public information on published events — no privacy concern.
- **No payment data:** Per DEC-1 and QG-6, EventHub stores no card data. Price and capacity are organizer-configured metadata only.

## 8. Edge Cases

**EC-01:** An organizer adds two ticket types with the same name. *Resolution:* Type names must be unique within an event. The system rejects the duplicate with a clear message.

**EC-02:** An organizer edits a ticket type's price after some orders have been placed at the old price. *Resolution:* Already-placed orders retain their snapshotted price (INV-25). Only future orders use the new price. No retroactive change.

**EC-03:** An organizer tries to remove the only remaining ticket type from a published event. *Resolution:* Blocked — INV-11 requires at least one ticket type for a published event. The organizer must unpublish or cancel first.

**EC-04:** An organizer reduces a ticket type's capacity to exactly the number sold (no remaining). *Resolution:* Allowed. The type immediately shows as sold out; no further purchases possible for that type.

**EC-05:** All ticket types on an event sell out. *Resolution:* The event page shows all types as sold out. The event itself is effectively sold out — no tickets can be purchased. The event status does not automatically change to Closed; the organizer must close or cancel explicitly.

**EC-06:** A free ticket type (price = 0) coexists with paid types. *Resolution:* Allowed per F-3.2. The free type auto-confirms on checkout (F-6.2); paid types go through payment (F-6.1). Each type is handled according to its own price.

**EC-07:** An attendee selects quantities from multiple types in a single order. *Resolution:* Allowed. Each type becomes a separate order line with its own unit price snapshot. The order total is the sum of all line totals.

## 9. Dependencies & Risks

**Dependencies (from features.md):**
- **F-3.1 (Define a ticket type):** Must exist — this feature extends the single-type model to multiple types.
- **F-3.3 (Transparent pricing):** Each type must show its all-inclusive price.
- **F-3.4 (Inventory and no-oversell):** The per-type inventory mechanism must work independently for each type.
- **F-4.1 (Public event page):** Must render multiple types.
- **F-5.1 (Select tickets and start checkout):** Must support multi-type selection.

**Risks:**
- **R-1: UI complexity.** Multiple types add visual weight to the event page and checkout. Mitigation: keep the default to one type (the common case); additional types are opt-in.
- **R-2: Inventory hot-spot amplification.** More types means more concurrent reservations on the same Event aggregate. At small-event scale (ASM-2), this is acceptable; the optimistic concurrency mechanism handles it.
- **R-3: Naming confusion.** If type names are unclear, attendees may be confused. Mitigation: organizers are responsible for clear names; the system enforces uniqueness but not descriptiveness.

## 10. Assumptions

- The Event aggregate and ENT-TicketType already support multiple types per event in the domain model (ddd.md). This feature activates and end-to-end tests that behavior.
- The organizer interface already allows adding at least one ticket type (F-3.1). This feature ensures the "add another" flow works smoothly.
- Maximum of 10 ticket types per event.

## 11. Out of Scope

- **Discount codes (F-3.7):** A separate feature that applies to the order total, not per type.
- **Per-order purchase limits (F-3.6):** Caps on total quantity per order, independent of types.
- **Scheduled on-sale windows (F-3.8):** Time-gated availability per type.
- **Ticket type ordering/display priority:** Types appear in creation order. Drag-to-reorder is not included.
- **Capacity allocation across types** (e.g., "total event capacity" shared across types): Each type has its own independent capacity.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should there be a maximum number of ticket types per event? | ✅ Resolved: 10 types max |
| 2 | Should the system support reordering ticket types on the public page? | ✅ Resolved: creation-order sufficient |
