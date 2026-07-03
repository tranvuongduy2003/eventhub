---
artifact_type: spec
artifact_version: 1
id: spec-20260703003908-select-tickets-and-start-checkout
title: Select tickets and start checkout
slug: select-tickets-and-start-checkout
filename_template: 20260703003908-select-tickets-and-start-checkout.md
created_at: "2026-07-03T00:39:08+07:00"
updated_at: "2026-07-03T02:21:14+07:00"
status: draft
owner: product
tags: [spec, eventhub, purchase-checkout]
feature_refs: ["F-5.1"]
ddd_refs: ["BC-2", "BC-3", "AGG-Event", "AGG-Order", "ENT-TicketType", "ENT-Reservation", "INV-10", "INV-14", "INV-21", "INV-24"]
prd_refs: ["DEC-1", "DEC-3", "QG-1", "QG-2", "QG-4", "QG-5", "QG-6"]
tech_refs: ["Tech Section 4", "Tech Section 5", "Tech Section 6", "Tech Section 7"]
db_refs: ["Tech Section 6"]
github_issue: "https://github.com/tranvuongduy2003/eventhub/issues/61"
search_index:
  keywords: [tickets, checkout, quantity, availability, order, attendee, buyer, purchase, limits, inventory, mobile]
  bounded_contexts: ["Event Management", "Sales"]
  user_personas: ["PER-A1", "PER-A2"]
---

# Feature: Select tickets and start checkout

> Features: F-5.1  |  Status: DRAFT  |  Date: 2026-07-03
> PRD: DEC-1, DEC-3, QG-1, QG-2, QG-4, QG-5, QG-6  |  DDD: BC-2 AGG-Event, BC-3 AGG-Order, INV-10, INV-14, INV-21, INV-24  |  Tech: Sections 4-7

## 1. Problem & Solution

**Problem:** An attendee can view a published event and its ticket types, but they still need a clear, low-friction way to choose what they want to buy. Without quantity selection and immediate validation, buyers may attempt impossible purchases, misunderstand availability, or enter checkout only to be blocked later.

**Solution:** From the public event page, an attendee selects quantities for one or more available ticket types and starts checkout only when the selection is valid. The experience keeps the buyer on the event page until the chosen quantities are within current availability, sales-window rules, and any configured per-order limit. Clear messages explain why a selection cannot proceed.

**Personas:** PER-A1 (general attendee) needs a fast mobile purchase with a clear price. PER-A2 (group buyer) needs to choose multiple tickets for friends without exceeding limits.

**Scope:** F-5.1 only. This slice includes choosing ticket quantities and entering checkout with a valid selection. It does not collect guest contact details (F-5.2), create the Pending order or reservation hold (F-5.3), show the final order summary (F-5.4), expire holds (F-5.5), show order status (F-5.6), or initiate/capture payment (EP-6).

## 2. Acceptance Criteria

**AC-01:** GIVEN a Published event with at least one ticket type currently available for sale, WHEN an attendee opens the event page, THEN each purchasable ticket type has quantity controls and a visible final all-inclusive unit price.

**AC-02:** GIVEN an attendee has selected zero tickets across all ticket types, WHEN they view the checkout action, THEN they cannot proceed and are told to select at least one ticket.

**AC-03:** GIVEN an attendee selects quantities greater than zero and every selected ticket type is within availability and applicable per-order limits, WHEN they start checkout, THEN the purchase flow begins with the selected event, ticket types, quantities, and displayed prices carried forward.

**AC-04:** GIVEN an attendee selects a quantity greater than the currently available quantity for a ticket type, WHEN they attempt to proceed or the quantity is validated, THEN the selection is blocked with a clear message that the requested quantity is not available.

**AC-05:** GIVEN a ticket type has a per-order limit, WHEN an attendee selects a quantity above that limit, THEN the selection is blocked with a clear message that states the maximum allowed for that ticket type.

**AC-06:** GIVEN multiple ticket types are available, WHEN an attendee selects quantities for more than one ticket type, THEN each ticket type is validated independently for availability and per-order limit, and the attendee can proceed only when all selected lines are valid.

**AC-07:** GIVEN a ticket type is sold out, outside its sales window, or otherwise not purchasable for the current event state, WHEN the attendee views the ticket type, THEN its quantity cannot be increased and the reason is visible.

**AC-08:** GIVEN availability changes after the event page was loaded, WHEN the attendee attempts to proceed with a stale quantity, THEN the selection is revalidated and either proceeds with the still-valid quantity or is blocked with an updated availability message.

**AC-09:** GIVEN two or more attendees race for the last remaining tickets, WHEN they attempt to proceed with overlapping quantities, THEN the system does not allow more tickets to be selected into checkout than can be validly reserved later; at most the valid remaining quantity may continue.

**AC-10:** GIVEN a ticket type has price zero, WHEN an attendee selects a valid quantity, THEN they can start checkout with a displayed unit price of zero and no payment is requested in this slice.

**AC-11:** GIVEN a paid ticket type, WHEN an attendee selects a valid quantity, THEN the selection displays enough price context for the buyer to understand what they selected, but this slice does not charge or capture payment.

**AC-12:** GIVEN an attendee is not signed in, WHEN they select valid quantities and start checkout, THEN they are allowed to continue as a guest; account creation is not required.

**AC-13:** GIVEN an event is Draft, Closed, Cancelled, or not found, WHEN an attendee attempts to start checkout from that event, THEN checkout is not started and the attendee receives a clear non-purchasable or not-found message.

**AC-14:** GIVEN validation fails for any selected ticket line, WHEN the attendee corrects the quantity to a valid value, THEN the blocking message clears and checkout can proceed if all other selected lines are valid.

**AC-15:** GIVEN the attendee uses a phone-sized viewport, WHEN they adjust ticket quantities and start checkout, THEN the controls remain usable without horizontal scrolling, hidden prices, or ambiguous disabled states.

**AC-16:** GIVEN an attendee has selected valid ticket quantities, WHEN they refresh the page, copy the page URL, or navigate away and return in the same browser session before a durable order exists, THEN the selected event and ticket quantities are preserved through the URL and browser session where possible and are revalidated before checkout continues.

## 3. Domain & Business Rules

- **Published event required:** Ticket selection is allowed only for events that are public and purchasable. Reservations later require the event to be Published, inside any sales window, and not Closed or Cancelled (INV-14).
- **No oversell promise:** The selection step must respect the same availability truth used by reservations so the purchase flow does not invite impossible purchases. The hard invariant remains `Reserved + Sold <= Capacity` per ticket type (INV-10).
- **Ticket type availability:** Availability is derived from the ticket type capacity minus sold and reserved quantities. Sold-out ticket types remain visible when appropriate, but cannot be selected.
- **Per-order limits:** If a ticket type has a maximum per-order quantity, the selected quantity must not exceed that limit (INV-24). If no limit is configured, capacity and availability are still enforced.
- **No reservation yet:** Starting checkout from this slice may carry a valid selection forward, but the Pending order and live reservation are owned by F-5.3. Until that later step succeeds, the buyer does not have a hold.
- **Transparent pricing:** Prices shown during selection are final all-inclusive prices. This supports QG-2 and prevents surprise charges later.
- **Guest purchase path:** The buyer may continue without an account. Guest name and email collection belongs to F-5.2, but this slice must not block anonymous attendees from entering that flow.

## 4. UI Behavior or API Contract

- The public event page presents each ticket type with name, final unit price, availability state, quantity controls, and any per-order maximum that affects the buyer.
- Availability is shown as buyer-facing states only, such as Available, Limited, or Sold out. The selection experience does not expose exact remaining ticket counts.
- Quantity controls support increasing, decreasing, and clearing selected quantities without allowing negative values.
- The checkout action remains unavailable or returns validation feedback until at least one valid ticket is selected.
- Validation messages identify the affected ticket type and the specific problem: no tickets selected, sold out, outside sales window, exceeds availability, exceeds per-order limit, or event no longer purchasable.
- When the attendee starts checkout successfully, the next purchase step receives the selected event and ticket lines. Product behavior requires the next step to revalidate before creating a hold.
- Valid selections are preserved in the URL and browser session before F-5.3 creates a durable order, so refresh, shareable navigation, and browser back/forward behavior do not silently discard the buyer's in-progress selection.
- API behavior, where exposed, must accept a public event identifier and selected ticket lines, then respond with either a checkout-start result or product-level validation errors. Responses must use contract DTOs and must not expose domain entities.

## 5. Data & Storage Impact

- **PostgreSQL:** Authoritative event, ticket type, sold, reserved, and sales-window state is read from PostgreSQL. No durable order or reservation record is required by F-5.1 alone.
- **Redis:** May cache public event or availability reads only if the cached data is rebuildable from PostgreSQL and revalidation protects against stale selections.
- **MinIO:** No new binary storage impact. Existing cover images on the event page may continue to be displayed.
- **RabbitMQ:** No integration event is required for quantity selection alone. Order and inventory events begin in F-5.3.
- **Payment provider:** No payment provider interaction occurs in this slice.

## 6. Real-Time & Consistency

Real-time updates are not required for F-5.1. Availability shown to the attendee may become stale while they are viewing the page, so starting checkout must revalidate against authoritative availability before continuing. Strong no-oversell enforcement remains with reservation creation in F-5.3, but this slice should fail early and clearly when a selection is already invalid.

## 7. Security & Privacy

- Public ticket selection is available to visitors without a session and does not require account creation.
- This slice collects no personal data. Guest contact details belong to F-5.2.
- The purchase path must not expose private organizer data, internal inventory records, or other buyers' details.
- Payment data is not collected or handled in this slice, consistent with DEC-1 and QG-6.
- Validation failures should not reveal sensitive implementation details; they should use attendee-readable product messages.

## 8. Edge Cases

**EC-01:** The attendee selects tickets, then another buyer takes the remaining availability before checkout starts; the attendee sees updated availability and must adjust.

**EC-02:** A ticket type sells out while the page is open; increasing quantity is blocked at checkout-start validation.

**EC-03:** The organizer closes or cancels the event while the page is open; checkout start is blocked with the current event status.

**EC-04:** A sales window opens after the page is loaded; refreshing or revalidation should allow selection once the ticket type is purchasable.

**EC-05:** A sales window closes after selection; checkout start is blocked with an unavailable message.

**EC-06:** The attendee attempts a negative, fractional, non-numeric, or otherwise malformed quantity; the quantity is rejected and no checkout starts.

**EC-07:** The attendee selects an extremely large quantity; the selection is capped or rejected based on availability and per-order limit.

**EC-08:** The event has multiple ticket types with mixed states, such as one available and one sold out; the attendee can proceed only with valid selected ticket lines.

**EC-09:** A free ticket type and a paid ticket type are both selected; the selection step carries both lines forward without deciding payment behavior.

**EC-10:** The attendee navigates back from the next checkout step; their valid ticket selection should remain understandable and editable through URL and browser-session preservation, then revalidated before proceeding again.

## 9. Dependencies & Risks

**Dependencies:** F-4.1 provides the public event page entry point. F-3.4 provides ticket availability and no-oversell behavior. F-3.6 provides per-order limits when configured. F-3.3 provides final all-inclusive prices shown before purchase.

**Risks:** Availability is naturally race-prone because multiple attendees may select tickets at the same time. The product risk is mitigated by revalidating before checkout starts and by relying on F-5.3 reservation creation for the final hold. Another risk is scope creep into order placement, contact collection, and payment; those remain explicitly outside this feature.

## 10. Assumptions

- Ticket quantities are whole non-negative integers.
- A buyer may select multiple ticket types in one checkout start.
- If F-3.6 has not yet shipped, per-order limit validation is inactive except where a limit already exists in the ticket type data.
- "Start checkout" means entering the purchase flow with a validated selection, not creating a durable order or holding inventory.
- The product uses one configured currency for money values.
- Mixed free and paid ticket selections continue through one unified checkout flow for MVP.

## 11. Out of Scope

- Guest name and email collection.
- Creating a Pending order or reservation hold.
- Order price summary, discounts, and final total confirmation.
- Payment initiation, capture, failure handling, or refunds.
- Ticket issuance, QR codes, email delivery, transfers, returns, and check-in.
- Organizer-facing sales dashboards or real-time sales monitoring.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the quantity control show exact remaining availability, or only states such as Available, Limited, and Sold out? | Answered: only states such as Available, Limited, and Sold out. |
| 2 | Should valid selections be preserved in the URL, browser session, or only in the current client state before F-5.3 creates an order? | Answered: preserve valid selections in the URL and browser session. |
| 3 | Should mixed free and paid ticket selections continue through one unified checkout flow in MVP? | Answered: yes. |
