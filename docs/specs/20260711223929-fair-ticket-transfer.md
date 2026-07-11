---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260711223929-fair-ticket-transfer
title: Fair Ticket Transfer
slug: fair-ticket-transfer
status: implemented
created_at: 2026-07-11
updated_at: 2026-07-11
owner: builder
language: en
feature_ids:
  - F-10.1
  - F-10.2
product_refs:
  - DEC-2
  - QG-3
technical_refs:
  - BC-5
  - AGG-Ticket
  - INV-42
  - INV-43
  - INV-44
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# Fair Ticket Transfer

## Problem and solution

Attendees need a fair way to pass a ticket to another person when plans change, without creating a paid resale surface. EventHub already issues tickets with unique codes and validates them at the door. This slice adds face-value transfer for an issued ticket: the current holder supplies a recipient contact, EventHub invalidates the old code, issues a fresh ticket to the recipient, and requests delivery to the recipient.

The implementation scope is F-10.1 and F-10.2 only. Return-to-pool behavior from F-10.3/F-10.4 remains out of scope because it depends on refund-on-cancellation/refund orchestration F-6.6, which is still planned.

## Acceptance criteria

**AC-01:** Given a valid, untransferred, unchecked-in ticket from a confirmed order, when the holder transfers it to a recipient name and email, then the original ticket status becomes `Transferred`.

**AC-02:** When a transfer succeeds, then a new valid ticket is issued for the same event, order, and ticket type with the recipient as holder and a fresh unique ticket code.

**AC-03:** When a transfer succeeds, then EventHub collects no money, stores no transfer price, and exposes no request field that can set a price.

**AC-04:** Given a checked-in ticket, when a transfer is requested, then the request is refused and no replacement ticket is issued.

**AC-05:** Given a transferred, void, unknown, or otherwise non-valid ticket, when a transfer is requested, then the request is refused and no replacement ticket is issued.

**AC-06:** Given a transfer succeeds, when the old code is scanned or manually checked in, then it is rejected as not valid for check-in.

**AC-07:** Given a transfer succeeds, when the new code is scanned for the matching event, then only the new holder's ticket can be accepted for entry.

**AC-08:** Given a transfer succeeds, then delivery is requested to the recipient email with the new ticket details.

## Domain and business rules

- Transfers are face-value only. No transfer amount, fee, payment, refund, or provider interaction exists in this slice.
- A ticket can be transferred only from `Valid` status.
- A checked-in ticket cannot be transferred.
- A transferred or void ticket cannot be transferred again and cannot be checked in.
- Transfer preserves event id, order id, and ticket type id. It changes only the holder and ticket code by creating a replacement ticket.
- The old ticket keeps its original code for auditability, but that code is invalid because the ticket status is no longer `Valid`.
- The replacement ticket receives a unique unguessable code from the existing ticket code generator.
- The transfer operation is atomic inside the command transaction: either the old ticket is invalidated and the new ticket is persisted, or neither change is committed.
- Domain events record the transfer fact for reporting and notification projections. Email delivery remains a side effect; PostgreSQL ticket state is authoritative.

## UI behavior or API contract

- Add an unauthenticated attendee-facing transfer endpoint under the existing ticket/order surface.
- The request accepts recipient name and recipient email only. It must not accept price, amount, currency, payment method, or fee fields.
- The response returns the replacement ticket summary using the same ticket response vocabulary already used for issued tickets.
- Expected failures use RFC 7807 ProblemDetails with stable error codes:
  - unknown ticket or non-matching order reference;
  - source order is not confirmed;
  - ticket already checked in;
  - ticket already transferred or otherwise not valid;
  - invalid recipient contact.
- Browser UI work is out of scope for this backend slice unless generated client verification requires contract refresh.

## Data

- Existing ticket fields are sufficient for the first slice: event id, order id, ticket type id, code, holder contact, status, issued time, checked-in time, delivery time, and row version.
- The replacement ticket is a separate row. The source ticket status becomes `Transferred`.
- No new payment, refund, inventory, or price data is introduced.
- Transfer-origin reporting fields are deferred unless a later reporting requirement needs a durable source-ticket reference on the replacement ticket.

## Security and privacy

- Ticket codes and order references are treated as accountless access credentials and must remain unguessable or no more exposed than existing ticket access behavior.
- The endpoint must not disclose unrelated holder data for unknown ticket/order combinations.
- Logs and error messages must not include full ticket codes.
- Recipient email is normalized using the existing `Contact` value object.
- Backend state, not frontend route visibility, enforces checked-in and transferred-ticket safeguards.

## Edge cases

- Unknown ticket id or code.
- Ticket belongs to another order/event than the transfer URL.
- Source order is pending, expired, cancelled, or refunded.
- Ticket is already checked in.
- Ticket is already transferred or void.
- Recipient name or email is invalid.
- Generated replacement code collides with an existing code and must be retried.
- Delivery succeeds or is delayed independently of committed transfer state.
- Concurrent transfer/check-in attempts resolve through the existing unit-of-work transaction and optimistic concurrency behavior; no state may commit that leaves both old and new tickets valid.

## Dependencies

- F-7.1 ticket issuance and unique ticket codes.
- F-7.2/F-7.3 delivery and accountless ticket access.
- F-8.1 check-in validation.
- Existing order confirmation state from EP-5/EP-6.

## Assumptions

- The first backend slice may use the existing order/ticket access shape rather than introducing a new opaque transfer token.
- Email delivery uses the existing `IEmailSender` and ticket email composer.
- Attendee identity is contact-email based; optional attendee accounts are not required for transfer.

## Out of scope

- F-10.3/F-10.4 return-to-pool, refund, sold-out eligibility, cutoff rules, and organizer approval flows.
- Any paid resale marketplace, price entry, transfer fee, provider payment, or platform fee.
- Browser transfer UI and e2e coverage unless a later frontend slice asks for it.
- Transfer audit screens, organizer dashboards, and reporting projections beyond the authoritative ticket state.

## Adjacent Feature Boundary

In scope: F-10.1/F-10.2 backend ticket transfer behavior, domain safeguards, REST contract, email delivery request, and automated evidence that old codes are invalid while new codes remain usable.

Out of scope: F-10.3/F-10.4 return-to-pool; refund workflows from F-6.6; public transfer UI; realtime transfer notifications.

Neighboring features: F-7.1 ticket issuance, F-7.2 delivery, F-7.3 access links, F-8.1 scan validation, F-8.2 duplicate prevention, F-6.6 refunds.

## Implementation evidence

- Domain unit tests cover successful transfer, checked-in transfer rejection, repeat-transfer rejection, and old-code check-in rejection.
- API integration tests cover successful transfer, no price fields in the public transfer request, recipient email delivery, persisted old/new ticket state, checked-in transfer rejection, old code rejection, and new code check-in.
- OpenAPI contract exposes only `recipientName` and `recipientEmail` on `TransferTicketRequest`.

## Harness Impact

N/A - product slice only; no harness behavior changes.
