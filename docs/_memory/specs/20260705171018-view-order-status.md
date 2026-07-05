---
title: View order status
feature_id: F-5.6
status: implemented
owner: Builder
created_at: 2026-07-05T17:10:18+07:00
updated_at: 2026-07-05T21:29:00+07:00
---

# F-5.6 - View Order Status

Status: implemented.

## Problem and solution

Attendees checking out as guests need a durable way to come back to an order without signing in. After an order is placed, EventHub should give them a simple order reference link they can reopen to see whether the order is pending, confirmed, expired, or cancelled.

The solution is a public, guest-friendly order status experience backed by the existing order status API. The page shows the order reference, status, line items, total, and relevant timestamps. Once ticket issuance exists in EP-7, confirmed orders will also surface issued tickets from this same order reference.

## Acceptance criteria

- After placing an order, the attendee sees a link to the order status page.
- Opening an existing order reference without signing in shows the order status: pending, confirmed, expired, or cancelled.
- The status view shows the same order line items and final total captured when the order was placed.
- Missing or unknown order references show a clear not-found state.
- Confirmed-ticket display is represented as a documented pending dependency until EP-7 issues tickets.

## Domain and business rules

- Guest order lookup is accountless and uses the order reference already returned by checkout.
- Order status values come from the Sales order lifecycle.
- The order summary remains based on order snapshots, not current ticket type prices.
- Ticket display is out of scope until EP-7 creates ticket records and access links.

## UI behavior or API contract

- The checkout success state includes a link to `/orders/{orderId}`.
- `/orders/{orderId}` fetches `GET /api/orders/{orderId}` and renders the status, placed time, optional confirmation time, line items, total, and discount if present.
- A not-found order returns a readable state with a path back to public events.
- No sign-in is required.

## Data, real-time, security, edge cases, dependencies, assumptions, out of scope

- Data: uses existing order, order line, and ticket type data. No new persistence is required for this slice.
- Real-time: N/A; manual refresh is acceptable.
- Security: this MVP exposes status by numeric order reference. Unguessable guest references can be added later if product risk rises.
- Edge cases: unknown order, expired order, cancelled order, pending paid order waiting on EP-6, confirmed order waiting on EP-7 tickets.
- Dependencies: F-5.3 order creation and F-5.4 final price summary.
- Assumption: EP-7 ticket issuance is not implemented yet, so the page shows a ticket placeholder for confirmed orders.
- Out of scope: payment provider redirects, ticket issuance, QR display, ticket recovery/resend, and attendee accounts.

## Adjacent Feature Boundary

Neighboring features: F-5.3 creates orders and holds inventory; F-5.4 owns the final price summary; F-5.5 may move pending orders to expired; F-6 confirms paid/free orders; EP-7 issues and displays tickets.

In this slice: the attendee can navigate to an order reference and see the current Sales order status and snapshot summary.

Out of scope: creating payments, issuing tickets, QR codes, email delivery, attendee wallets, and new authorization rules.

## 7. Harness Impact

N/A - product slice only; no harness behavior changes.
