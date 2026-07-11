---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260705160617-final-price-summary
title: Final price summary
slug: final-price-summary
status: implemented
created_at: 2026-07-05T16:06:17+07:00
updated_at: 2026-07-05T16:31:00+07:00
owner: builder
language: en
feature_ids: []
product_refs:
  - DEC-1
  - DEC-3
  - QG-2
  - QG-4
  - QG-6
technical_refs:
  - "Tech Section 4"
  - "Tech Section 7"
  - "Tech Section 12"
  - BC-3
  - AGG-Order
  - ENT-OrderLine
  - INV-20
  - INV-25
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# F-5.4 - Final Price Summary

> Features: F-5.4 | Status: IMPLEMENTED | Date: 2026-07-05
> PRD: DEC-1, DEC-3, QG-2, QG-4, QG-6 | DDD: BC-3 AGG-Order, ENT-OrderLine, INV-20, INV-25 | Tech: Sections 4, 7, 12

## Problem and solution

Attendees need a trustworthy final summary before payment. The price shown at checkout must match the order total that will be charged, with no hidden fees or later surprises.

The solution is to show the placed order's authoritative line items, any applied discount, and final all-inclusive total after the inventory hold is created and before any payment step begins. The summary uses the order's price snapshots rather than recalculating from current ticket type prices, so later organizer edits cannot change what this attendee is about to pay.

## Acceptance criteria

**AC-01:** Given an attendee places an order, when the order is accepted, then the attendee sees each ordered ticket line with a ticket type name, quantity, unit price, and line total.

**AC-02:** Given the order has no discount, when the summary is shown, then the final total equals the sum of order line totals.

**AC-03:** Given a valid discount code is applied, when the summary is shown, then the discount code and discount amount are shown and the final total equals the discounted order total.

**AC-04:** Given a free order or a discount that reduces the total to zero, when the summary is shown, then the final total is zero and no fee is added.

**AC-05:** Given ticket type prices change after the order is placed, when the attendee views the accepted order summary, then the line prices and final total remain the order's original snapshots.

## Domain and business rules

- Sales owns the order and final checkout total.
- Order lines must use unit price snapshots captured at placement.
- `INV-20` remains authoritative: final total is line subtotal minus any discount, never below zero.
- EventHub adds no platform fee at launch, per `DEC-1`.
- The summary is a display of an already placed Pending or Confirmed order; it must not mutate price, inventory, payment, or discount state.

## UI behavior or API contract

- The checkout accepted-order view shows the authoritative order summary before payment.
- Each line includes a buyer-readable ticket type name, quantity, unit price, and line total.
- The summary displays any applied discount and the final all-inclusive total returned by the order API.
- API responses use Contracts DTOs and never expose domain entities.
- Payment initiation remains a later step and must use the same final total.

## Data

- No new storage is required.
- The API may join order line ticket type identifiers to ticket type names for display.
- Order line prices and totals come from order persistence, not current ticket type prices.

## Real-time

N/A for this slice. Realtime sales monitoring is owned by EP-11.

## Security

The accepted-order summary contains only the attendee's own just-created order data. Payment card or provider data is not collected or shown in this slice.

## Edge cases

- Multiple ticket types appear as separate line items.
- Repeated order lines for the same ticket type must still produce a correct total.
- Unknown or removed ticket type display data must not change persisted order totals.
- Discount amounts larger than subtotal are clamped to zero total.
- Free orders may already be Confirmed, but the summary still displays the final zero total.

## Dependencies

- F-5.3 creates the Pending order, reservation, line price snapshots, and total.
- F-3.3 defines transparent all-inclusive pricing.
- F-3.7 may provide discount codes when present.
- EP-6 will later initiate payment from the same order total.

## Assumptions

- The MVP uses a single configured currency.
- The accepted checkout view can use the order placement response as the immediate summary source.
- Public order lookup for later recovery remains owned by F-5.6.

## Out of scope

- Payment initiation, capture, failure, and refunds.
- Hold expiry and release behavior.
- Ticket issuance or delivery.
- Organizer sales reporting.
- A standalone order status page.

## Adjacent Feature Boundary

**Neighboring features:** F-5.1 selects tickets and starts checkout; F-5.2 collects guest contact; F-5.3 creates the order and inventory hold; F-5.5 expires holds; F-5.6 shows order status; EP-6 handles payment.

**In this slice:** Present the authoritative final order summary with named line items, discounts, and all-inclusive total before payment.

**Out of this slice:** Creating new payment behavior, changing reservation semantics, order recovery links, ticket delivery, and realtime monitoring.

## 7. Harness Impact

N/A - product slice only; no harness behavior changes.

| Lane | Impact |
|------|--------|
| `scripts/agent/` | N/A |
| `.codex/agents/` | N/A |
| `.codex/policies/` or `.codex/hooks/` | N/A |
| `.codex/tmp/telemetry/` | N/A |
| `scripts/agent/` | N/A |
| Workflow surfaces | N/A |

