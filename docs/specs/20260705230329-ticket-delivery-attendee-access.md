---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260705230329-ticket-delivery-attendee-access
title: Ticket Delivery & Attendee Access
slug: ticket-delivery-attendee-access
status: implemented
created_at: 2026-07-05
updated_at: 2026-07-05
owner: builder
language: en
feature_ids:
  - F-7.1
  - F-7.2
  - F-7.3
  - F-7.4
  - F-7.5
  - F-7.6
product_refs: []
technical_refs:
  - BC-5
  - BC-6
  - AGG-Ticket
  - VO-TicketCode
  - EVT-OrderConfirmed
  - EVT-TicketIssued
  - INV-40
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# EP-7 - Ticket Delivery & Attendee Access

## Problem and solution

Confirmed orders currently stop at order status. Attendees need the next usable artifact in the event journey: valid tickets that can be opened without an account, shown clearly on a phone, and delivered or recovered through email.

This slice turns each confirmed purchased unit into an issued ticket with a unique scannable code, makes the buyer's ticket link usable without signing in, requests email delivery after issuance, supports resend, and gives signed-in attendees a wallet view for tickets bought with their email.

## Acceptance criteria

**AC-01:** Given an order becomes Confirmed, when ticket issuance runs, then one ticket is created for each purchased unit.

**AC-02:** Each issued ticket has a unique scannable code tied to the event, order, ticket type, and buyer contact.

**AC-03:** Given the same confirmation is observed more than once, when ticket issuance runs again, then no duplicate tickets are created.

**AC-04:** When tickets are issued, then delivery is requested to the buyer email with event details, ticket details, QR/scannable codes, and the accountless ticket link.

**AC-05:** Given email delivery is processed shortly after confirmation, when the buyer checks the order or ticket link, then issued tickets are still available from PostgreSQL even if email is delayed.

**AC-06:** Given a buyer opens an order or ticket reference link, when the order is confirmed and tickets exist, then the buyer can view all QR tickets without signing in.

**AC-07:** On a phone-sized viewport, each ticket is individually viewable and its QR/scannable code is large and clear enough for door scanning.

**AC-08:** Given a buyer requests resend with an order reference and matching email, when tickets exist, then EventHub requests delivery again without issuing new tickets.

**AC-09:** Given a resend request does not match an issued-ticket order, when it is submitted, then the response does not reveal whether the order or email exists.

**AC-10:** Given a signed-in attendee account, when they open their wallet, then they see tickets bought with their account email across events.

## Domain and business rules

- Tickets belong to the Ticketing bounded context and are authoritative records in PostgreSQL.
- A ticket is issued from a confirmed order only; pending, expired, cancelled, or refunded orders do not produce valid tickets in this slice.
- Ticket codes must be unique and unguessable enough for attendee links and QR payloads.
- Issuance is idempotent per order: after tickets exist for an order, later confirmation handling returns the existing ticket set.
- Ticket delivery is a notification side effect, not the source of truth. Lost or delayed email must not lose tickets.
- Resend redelivers existing tickets; it never creates, voids, transfers, or checks in tickets.
- The wallet is matched by the signed-in attendee's normalized email. Buying still works as guest checkout.

## UI behavior or API contract

- The confirmed order status view links to the buyer's ticket display.
- The ticket display is public by order/ticket reference and requires no account.
- Each ticket display shows event name, event date/time, location, ticket type, buyer/holder contact, and a large QR/scannable code.
- The resend endpoint accepts an order reference plus email and returns a neutral accepted response.
- The attendee wallet endpoint is authenticated and returns ticket summaries grouped by event or listed in a scan-friendly order.
- HTTP responses use Contracts DTOs and RFC 7807 errors for invalid public references.

## Data, real-time, security, edge cases, dependencies, assumptions, out of scope

**Data:** Tickets store event id, order id, ticket type id, code, buyer/holder contact, status, timestamps, and an optional delivery timestamp. PostgreSQL remains authoritative.

**Real-time:** No SignalR behavior changes in this slice.

**Security:** Ticket codes are unique QR payloads. This MVP builds on the existing accountless order reference from F-5.6 for grouped ticket viewing; a future hardening slice may replace order-id links with opaque order access tokens. Resend must avoid account/order enumeration. Wallet access uses the current signed-in attendee identity.

**Edge cases:** Duplicate order confirmation, partial line quantities, zero-total orders, paid-order confirmation, delayed email, resend before issuance, unknown public reference, and mobile display of multiple tickets are covered.

**Dependencies:** F-5.6 order status, F-6.2 free-order confirmation, and F-6.3 paid confirmation must already exist.

**Assumptions:** The local email adapter can remain no-op while tests use a fake sender at the Application port. A QR can be rendered from the ticket code in the web client without changing the backend storage contract.

**Out of scope:** EP-8 scan validation/check-in, EP-10 transfer/returns, refund/cancellation invalidation, a production email provider, and email template design beyond useful local content.

## Adjacent Feature Boundary

In scope: F-7.1 through F-7.6 ticket issuance, delivery request, accountless ticket access, mobile ticket display, resend/recovery, and attendee wallet.

Out of scope: EP-8 check-in validation, EP-10 transfer/returns, refund-triggered invalidation, production email provider setup, and organizer attendee reporting.

Neighboring features: F-5.6 order status, F-6.2/F-6.3 confirmation, EP-8 check-in, EP-9 attendee lists, and EP-10 fair transfer.

## 7. Harness Impact

N/A - product slice only; no harness behavior changes.

| Lane | Impact |
|------|--------|
| `scripts/agent/` | N/A - no eval runner or case changes. |
| `.codex/agents/` | N/A - no orchestration contract changes. |
| `.codex/policies/` / `.codex/hooks/` | N/A - no policy or approval changes. |
| `.codex/tmp/telemetry/` | N/A - no harness telemetry changes. |
| `scripts/agent/` | N/A - no harness tool adapter changes. |
| Workflow surfaces | N/A - no graph, hook, skill, script, or AGENTS.md changes. |

