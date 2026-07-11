---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260704203404-create-order-hold-inventory
title: Create order and hold inventory
slug: create-order-hold-inventory
status: implemented
created_at: 2026-07-04T20:34:04+07:00
updated_at: 2026-07-04T21:05:00+07:00
owner: builder
language: en
feature_ids: []
product_refs: []
technical_refs:
  - BC-2
  - BC-3
  - AGG-Event
  - AGG-Order
  - ENT-Reservation
  - INV-10
  - INV-21
  - INV-25
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# F-5.3 - Create Order And Hold Inventory

> Features: F-5.3 | Status: IMPLEMENTED | Date: 2026-07-04
> PRD: DEC-3, QG-1, QG-4, QG-5 | DDD: BC-2 BC-3 AGG-Event AGG-Order, ENT-Reservation | Tech: Sections 4, 6, 7

## Problem and solution

Attendees who have selected tickets need a durable order before payment, and the selected inventory must be held so another buyer cannot take the same tickets while checkout continues.

The solution is to place a Pending order and create time-limited reservations on the event inventory in the same transactional boundary. Inventory remains truthful because availability is computed from capacity minus sold minus reserved, and reservation creation is guarded by the event aggregate's no-oversell invariant.

## Acceptance criteria

**AC-01:** Given a published event with available inventory, when a guest places an order with valid contact details and ticket quantities, then a Pending order is created with line items and price snapshots.

**AC-02:** Given the same successful placement, when the order is persisted, then the chosen quantity is reserved on the matching ticket type so other buyers cannot reserve it.

**AC-03:** Given a successful paid Pending order, when the order is persisted, then the order and reservation both carry a finite hold expiry.

**AC-04:** Given availability is reduced by an existing reservation, when another buyer views or starts checkout, then availability reflects the reserved quantity and does not overstate what remains.

**AC-05:** Given the requested quantity exceeds current availability, when the attendee places an order, then no order or reservation is created and the attendee receives a clear validation error.

## Domain and business rules

- Pending orders belong to Sales (`BC-3`) and reference the event, buyer contact, line items, total, and reservation.
- Reservations belong to Event Management (`BC-2`) and reserve a specific ticket type and quantity for an order until the hold expires.
- `INV-10` remains authoritative: `Reserved + Sold <= Capacity` per ticket type.
- `INV-21` requires a Pending order to reference a live reservation.
- Order lines snapshot prices at placement so later ticket-type edits do not change the placed order.
- The default hold duration for this MVP slice is 15 minutes.

## UI behavior or API contract

The existing guest order placement endpoint creates the order and returns the placed order summary. This slice does not add a new checkout screen. The public event and checkout-start flows must continue to compute availability from the authoritative reserved and sold counts.

## Data

- Persist the Pending order, order lines, order status, total, buyer contact, and hold expiry.
- Persist one or more reservations tied to the order and event ticket types.
- Update ticket-type reserved counts in the same unit of work.

## Real-time

N/A for this slice. Live sales and inventory updates are owned by F-11.1.

## Security

Public guest checkout remains allowed. Contact validation and ticket selection validation run through the Application command handler and validator. No payment data is collected in this slice.

## Edge cases

- Multiple requested lines for the same ticket type are aggregated for availability and reservation.
- A sold-out or otherwise unavailable ticket type rejects placement without partial reservation.
- Closed, cancelled, draft, or unknown events do not create orders.
- Free-order auto-confirmation may immediately commit the reservation; paid orders remain Pending until payment.

## Dependencies

- F-5.1 provides a valid selected-ticket handoff into checkout.
- F-3.4 provides reservation and no-oversell behavior on the Event aggregate.
- F-5.2 provides guest contact collection.

## Assumptions

- The MVP hold duration remains fixed at 15 minutes until a later configuration feature changes it.
- Multi-ticket-type orders may create multiple event reservations, while the order stores the primary reservation reference already used by the current model.

## Out of scope

- Payment initiation and capture (EP-6).
- Hold expiry job behavior and release UX (F-5.5), except that this slice must store the hold expiry needed by that later behavior.
- Public order status viewing (F-5.6).
- Realtime inventory broadcasting (F-11.1).

## Adjacent Feature Boundary

**Neighboring features:** F-5.1 selects tickets and starts checkout; F-5.2 collects guest buyer contact; F-5.4 displays the final price summary; F-5.5 expires holds and releases reservations; F-5.6 shows order status; EP-6 handles payment.

**In this slice:** Create the Pending order, persist its line items and hold deadline, reserve selected inventory, and keep availability truthful for other buyers.

**Out of this slice:** Payment, ticket issuance, order recovery links, hold-expiry notifications, realtime updates, and new checkout UI.

## 7. Harness Impact

N/A - product slice only; no harness behavior changes.

| Lane                                  | Impact |
| ------------------------------------- | ------ |
| `scripts/agent/`                      | N/A    |
| `.codex/agents/`                      | N/A    |
| `.codex/policies/` or `.codex/hooks/` | N/A    |
| `.codex/tmp/telemetry/`               | N/A    |
| `scripts/agent/`                      | N/A    |
| Workflow surfaces                     | N/A    |
