---
artifact_type: spec
artifact_version: 1
id: spec-20260626160000-per-order-purchase-limit
title: Per-order purchase limit
slug: per-order-purchase-limit
filename_template: 20260626160000-per-order-purchase-limit.md
created_at: "2026-06-26T16:00:00Z"
updated_at: "2026-06-26T16:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, ticketing]
feature_refs: [F-3.6, F-3.1, F-5.1]
ddd_refs: [BC-2, BC-3, AGG-Event, AGG-Order, ENT-TicketType, INV-24]
prd_refs: [QG-1, QG-3, QG-5]
tech_refs: [Tech §4, Tech §6]
db_refs: [Tech §6]
github_issue: 43
search_index:
  keywords: [purchase limit, per-order limit, max per order, ticket quantity cap, fair access, anti-hoarding, checkout validation, ticket type]
  bounded_contexts: [Event Management, Sales]
  user_personas: [PER-O1]
---

> GitHub: #43 (https://github.com/tranvuongduy2003/eventhub/issues/43)

# Feature: Per-order purchase limit

> Features: F-3.6  |  Status: DRAFT  |  Date: 2026-06-26
> PRD: QG-1 (simplicity), QG-3 (fairness), QG-5 (correct at small scale)
> DDD: BC-2 Event Management, BC-3 Sales, AGG-Event, AGG-Order, ENT-TicketType, INV-24

## 1. Problem & Solution

**Problem:** Without a per-order purchase limit, a single buyer could hoard a large number of tickets in one order — especially problematic for small events with limited capacity. This undermines fair access for other attendees and can lead to a poor experience when one person buys out most of the inventory.

**Solution:** Allow the organizer to set an optional maximum quantity per ticket type per order. When a limit is set, the checkout process enforces it: an attendee cannot add more units of that ticket type than the limit allows in a single order. The limit is configured at the ticket type level, giving organizers fine-grained control (e.g., "General: max 4 per order, VIP: max 2 per order").

**Personas:** PER-O1 (individual organizer) — wants to ensure fair access to tickets for small events where one large buyer could exhaust inventory.

**Scope:**
- **In:** Setting an optional per-order limit on each ticket type; enforcing the limit during checkout; displaying the limit to attendees; blocking orders that exceed the limit.
- **Out:** Per-event aggregate limits (summing across all types), discount codes (F-3.7), scheduled on-sale windows (F-3.8), group-buy workflows (PER-A2).

## 2. Acceptance Criteria

**AC-01:** GIVEN I hold the Owner role for the event (F-1.5), WHEN I set a per-order limit (a positive integer) on a ticket type, THEN the limit is saved and applied to all future orders for that type.

**AC-02:** GIVEN I hold the Owner role for the event, WHEN I clear or remove the per-order limit on a ticket type, THEN there is no per-order cap for that type (unlimited quantity per order, subject only to availability).

**AC-03:** GIVEN a ticket type has a per-order limit of 4, WHEN an attendee attempts to add 5 units of that type to their order, THEN the system prevents the addition and shows a clear message stating the maximum allowed per order for that type.

**AC-04:** GIVEN a ticket type has a per-order limit of 4, WHEN an attendee adds 4 units of that type to their order, THEN the addition succeeds and the attendee can proceed to checkout.

**AC-05:** GIVEN a ticket type has a per-order limit and the attendee has already selected some units of that type, WHEN they attempt to increase the quantity beyond the limit, THEN the system prevents the increase with a clear message.

**AC-06:** GIVEN an event with multiple ticket types each having different per-order limits, WHEN an attendee builds an order, THEN each type's limit is enforced independently (e.g., max 4 General + max 2 VIP in the same order is allowed).

**AC-07:** GIVEN a ticket type with a per-order limit of 4 and only 2 remaining in inventory, WHEN an attendee attempts to add tickets, THEN the effective cap is the lesser of the limit and the available quantity (2 in this case), and a message explains the constraint.

**AC-08:** GIVEN I do not hold the Owner role for the event, WHEN I attempt to set or change a per-order limit, THEN I am refused with an "insufficient permissions" message.

**AC-09:** GIVEN a ticket type with a per-order limit, WHEN an attendee views the public event page (F-4.1), THEN the limit is displayed alongside the ticket type so the attendee knows the cap before starting checkout.

**AC-10:** GIVEN I hold the Owner role, WHEN I set the per-order limit to zero or a negative number, THEN the system rejects the input with a clear message requiring a positive integer.

**AC-11:** GIVEN I hold the Owner role, WHEN I set the per-order limit to 1 on a ticket type, THEN each order may purchase at most 1 unit of that type — useful for limited-access items like workshop seats.

## 3. Domain & Business Rules

**Bounded context:** BC-2 — Event Management (limit is defined on the ticket type) and BC-3 — Sales (limit is enforced at order placement).

**Domain model alignment (from domain-model-specification.md):**
- `ENT-TicketType` already carries an optional `MaxPerOrder` attribute. This feature activates and end-to-end tests that attribute.
- **INV-24:** Line quantities must respect `MaxPerOrder` at placement. An order that violates this invariant cannot be created.

**Behavior:**
- The organizer sets `MaxPerOrder` on a ticket type via the `ChangeTicketType` behavior on the Event aggregate.
- `MaxPerOrder` is optional — when not set (null), there is no per-order cap for that type.
- When set, the value must be a positive integer (≥ 1).
- The limit is enforced during the `Place` behavior on the Order aggregate: each order line's quantity must be ≤ the corresponding ticket type's `MaxPerOrder` (if set).

**Interaction with availability (INV-10):**
- The per-order limit and inventory availability are independent constraints. Both must be satisfied.
- Effective maximum = min(MaxPerOrder, Available). The more restrictive of the two governs.

**Price snapshot (INV-25):**
- The `MaxPerOrder` value is not snapshotted on the order — it is a selling constraint, not a pricing attribute. The organizer may change it between orders.

**Events:** No new domain or integration events are required. The limit is a passive constraint checked at order placement, not a state transition.

## 4. UI Behavior

**Organizer — Ticket type edit screen:**
- Each ticket type shows an optional "Max per order" field (numeric input).
- When empty or null, no limit is applied — the field displays a placeholder like "No limit".
- When set, the value is displayed as "Max {n} per order" next to the type.
- Validation: the field rejects zero, negative numbers, and non-integer input with inline error messages.
- The field is editable for Draft and Published events (changes take effect on future orders only).

**Public event page (EP-4):**
- Each ticket type that has a per-order limit displays the limit beneath the type name and price, e.g., "Max 4 per order".
- Types without a limit show no additional text.

**Checkout (EP-5):**
- The quantity selector for each ticket type respects the per-order limit. If a limit is set, the maximum selectable quantity is capped at that limit (or available inventory, whichever is lower).
- If the attendee manually enters a quantity exceeding the limit, an inline error message appears: "Maximum {n} tickets per order for {type name}".
- The order summary before confirmation reflects the enforced quantities.

## 5. Data & Storage Impact

**PostgreSQL (app schema):**
- The `ticket_types` table already has a `max_per_order` column (nullable integer) from the initial schema (domain-model-specification.md ENT-TicketType). No schema change is required.
- If the column does not yet exist, a migration adds `max_per_order INTEGER NULL` to the `ticket_types` table.

**Redis:** No direct impact. The limit is read from PostgreSQL as part of the ticket type data during order placement.

**MinIO:** No impact.

**RabbitMQ:** No new messages. The limit is a passive validation rule, not an event-producing state change.

## 6. Real-Time & Consistency

**Consistency:**
- The per-order limit is checked at order placement time, within the same transaction that reserves inventory (the pragmatic two-aggregate write described in domain-model-specification.md §8). There is no eventual-consistency concern — the limit is a point-in-time validation.
- If the organizer changes the limit between a browse and a purchase, the attendee sees the current limit on the event page. The limit at placement time is what matters.

**Real-time (EP-11 — future):**
- No impact. The limit does not produce live data; it is a constraint.

## 7. Security & Privacy

- **Authorization:** Setting or changing the per-order limit requires the Owner role for the event (F-1.5). Staff users cannot modify ticket type settings.
- **Enforcement:** The limit is enforced server-side at order placement. Client-side capping (quantity selector) is a UX convenience, not a security measure — the server always validates.
- **No sensitive data:** The limit is a public attribute of the ticket type, visible to all attendees on the event page.

## 8. Edge Cases

**EC-01:** An attendee browses the event page, sees a limit of 4, then the organizer changes it to 2 before the attendee completes checkout. *Resolution:* The server enforces the limit at placement time (now 2). The attendee sees an error if they still have 4 selected; they must reduce to 2.

**EC-02:** A ticket type has a per-order limit of 4 but only 1 ticket remaining. *Resolution:* The effective cap is 1 (the lesser of limit and availability). The attendee can buy at most 1.

**EC-03:** An attendee places an order for 3 tickets (within the limit), then tries to place a second order for 3 more of the same type. *Resolution:* Each order is evaluated independently against the per-order limit. The second order is allowed if inventory permits. The limit prevents hoarding in a single order, not across multiple orders.

**EC-04:** An organizer sets the per-order limit to 1 for a workshop seat type. *Resolution:* Each order may contain at most 1 unit of that type. An attendee wanting 2 must place two separate orders (if inventory allows).

**EC-05:** The per-order limit is set, then the organizer removes it (sets to null). *Resolution:* Future orders have no cap for that type. Existing orders are unaffected.

**EC-06:** A free ticket type has a per-order limit. *Resolution:* The limit applies regardless of ticket price. Free tickets can also be capped per order.

**EC-07:** An attendee selects 2 of Type A (limit 4) and 3 of Type B (limit 2) in one order. *Resolution:* Type A is within limit; Type B exceeds its limit. The order is blocked; the attendee must reduce Type B to 2 or fewer.

## 9. Dependencies & Risks

**Dependencies (from feature-specification.md):**
- **F-3.1 (Define a ticket type):** Must exist — the per-order limit is an attribute on a ticket type.
- **F-5.1 (Select tickets and start checkout):** Must enforce the limit during ticket selection and checkout.

**Risks:**
- **R-1: Attendee confusion.** If limits are not clearly communicated, attendees may be surprised when blocked. Mitigation: display the limit prominently on the event page and in the checkout flow (AC-09).
- **R-2: Organizer misuse.** Setting a limit of 1 on general admission may frustrate group buyers (PER-A2). Mitigation: the field is optional and the organizer's responsibility; the system provides the tool, not the policy.
- **R-3: Multiple-order circumvention.** A determined buyer can bypass the per-order limit by placing multiple orders. Mitigation: accepted for this feature scope — the limit raises the floor for fair access without being a hard anti-hoarding mechanism. Account-level or email-level caps are out of scope.

## 10. Assumptions

- The `max_per_order` column already exists on the `ticket_types` table or will be added via migration as part of this feature.
- The organizer can set the limit at any time (Draft or Published); changes apply to future orders only.
- The limit is per ticket type per order, not per event or per attendee.
- The limit is a soft cap — it prevents large single-order hoarding but does not prevent multiple orders by the same person.

## 11. Out of Scope

- **Per-event aggregate limits** (e.g., "max 6 tickets total across all types"): Not included. Each type has its own independent limit.
- **Per-attendee or per-email caps** across multiple orders: Not included. The limit is per order only.
- **Discount codes (F-3.7):** A separate feature; the per-order limit applies regardless of whether a discount code is used.
- **Scheduled on-sale windows (F-3.8):** A separate feature; the limit applies whenever the type is on sale.
- **Dynamic limits** (e.g., "limit increases as event date approaches"): Not included. The limit is a static value set by the organizer.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the per-order limit be visible to attendees on the event page, or only enforced at checkout? | ✅ Resolved: visible on the event page (AC-09) for transparency |
| 2 | Should there be a system-wide maximum for the per-order limit (e.g., cap at 20)? | ✅ Resolved: No — keep it simple; the organizer decides the appropriate limit per type |
