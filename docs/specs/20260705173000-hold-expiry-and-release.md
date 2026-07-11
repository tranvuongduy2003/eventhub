---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260705173000-hold-expiry-and-release
title: Hold expiry and release
slug: hold-expiry-and-release
status: implemented
created_at: 2026-07-05
updated_at: 2026-07-05
owner: builder
language: en
feature_ids: []
product_refs:
  - QG-1
  - QG-5
  - DEC-3
technical_refs:
  - BC-2
  - BC-3
  - AGG-Event
  - AGG-Order
  - ENT-Reservation
  - INV-10
  - INV-21
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# F-5.5 - Hold Expiry and Release

Status: implemented.

## Problem and solution

Attendees can place paid orders that hold event inventory while payment is unfinished. If a buyer abandons checkout, that hold must not keep tickets unavailable forever.

The solution is a system-driven expiry flow: once a Pending order's hold window has passed without payment, EventHub marks the order Expired and releases the matching reservation so the tickets return to availability.

## Acceptance criteria

**AC-01:** Given a Pending order whose hold window has passed without payment, when expiry processing runs, then the order is marked Expired.

**AC-02:** Given that expired order has a live reservation, when expiry processing runs, then the reservation is released and the reserved quantity returns to availability.

**AC-03:** Given an order is Confirmed, Cancelled, already Expired, or has a future hold expiry, when expiry processing runs, then the order and inventory are not changed.

**AC-04:** Given multiple due Pending orders exist, when expiry processing runs, then each due order is handled without blocking unrelated events or future holds.

## Domain and business rules

- Pending orders belong to Sales (`BC-3`) and may expire only from Pending status.
- Reservations belong to Event Management (`BC-2`) and are the source of reserved inventory.
- Releasing a reservation must preserve `INV-10`: reserved plus sold cannot exceed capacity, and availability is recomputed from capacity minus reserved minus sold.
- Expiry is system-driven; it is not a public attendee or organizer action.
- Expiry is based on a UTC hold deadline.

## UI behavior or API contract

No new public UI or API route is required in this slice. Existing order status reads should show the updated Expired status after processing.

## Data, real-time, security, edge cases, dependencies, assumptions, out of scope

**Data:** Existing order status, order expiry timestamp, event reservation, and ticket type reserved counts are updated in PostgreSQL.

**Real-time:** No new realtime channel is required. Future availability views may observe the changed inventory through existing reads.

**Security:** The expiry flow runs as a trusted system process and does not expose a new authorization surface.

**Edge cases:** Confirmed orders must not expire. Missing or already-released reservations should not corrupt inventory. Future holds must remain untouched.

**Dependencies:** F-5.3 creates Pending orders with finite hold windows and live reservations.

**Assumptions:** The local clock source is UTC and injectable for tests.

**Out of scope:** Payment abandonment UI, retry payment flows, payment failure handling from F-6.4, order-status link work from F-5.6, and ticket issuance.

## Adjacent Feature Boundary

**Neighboring features:** F-5.3 creates the Pending order and reservation. F-5.4 displays the final summary. F-5.6 displays order status. F-6.1 through F-6.4 handle payment initiation, capture, and failure.

**In this slice:** Expire due Pending orders and release their inventory holds.

**Out of scope:** Creating holds, charging payments, confirming orders, issuing tickets, or adding attendee-facing retry-payment behavior.

## 7. Harness Impact

N/A - product slice only; no harness behavior changes.
