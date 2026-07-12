---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: eventhub.spec.pending-feature-completion
title: Pending Feature Completion
status: implemented
last_updated: 2026-07-12
owner: builder
language: en
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
applies_to:
  - F-6.6
  - F-8.5
  - F-8.6
  - F-10.3
  - F-10.4
  - F-11.2
  - F-11.3
---

# Pending Feature Completion

## Problem

`docs/pending-features.md` identified remaining planned features without dedicated implementation evidence: refund on cancellation, multi-device/offline check-in tolerance, return-to-pool, live check-in progress, and sold-out/low-stock nudges. Existing source already implemented the broader platform spine, but these final planned items needed explicit behavior, code, tests, and status reconciliation.

## Scope

In scope:

- F-6.6: cancelling an event refunds captured paid orders and invalidates issued tickets.
- F-8.5: concurrent or repeated check-in attempts preserve exactly-once admission.
- F-8.6: offline scan queues reconcile through an idempotent batch sync endpoint when connectivity returns.
- F-10.3/F-10.4: a holder can return an eligible ticket to a sold-out ticket type before event start; the ticket is voided, inventory returns, and payment is refunded when present.
- F-11.2: authorized check-in clients can receive live door-count updates over SignalR.
- F-11.3: live sales/inventory payloads identify sold-out and low-stock ticket types for owner/staff nudges.

Out of scope:

- A production payment provider integration beyond the existing local provider boundary.
- A full offline-first browser/PWA scanner UI.
- Custom organizer-configurable low-stock thresholds.

## Business Rules

- Refunds are idempotent at the payment aggregate: already-refunded payments are not refunded twice.
- Event cancellation voids valid issued tickets so cancelled-event tickets cannot be admitted.
- Batch check-in sync processes up to 100 queued scans and rejects duplicate codes in the same batch after the first accepted scan.
- Return-to-pool is allowed only while the ticket type is sold out and before event start.
- Low stock is true when remaining inventory is greater than zero and at or below `min(3, 20% of capacity)`, with a minimum threshold of one.
- SignalR hub joins reuse the same session and event-scoped permission model as REST.

## Acceptance Criteria

- GIVEN an event with captured paid orders, WHEN the owner cancels the event, THEN captured payments are refunded, affected orders are marked refunded, and issued tickets are voided.
- GIVEN duplicate scans arrive from multiple devices or a queued offline batch, WHEN the server reconciles them, THEN only the first scan admits the ticket and later scans are rejected with a stable reason.
- GIVEN a sold-out ticket type before event start, WHEN the holder returns a valid ticket, THEN the ticket is voided, the order is marked refunded, and one unit returns to availability.
- GIVEN a ticket type is not sold out or event start has passed, WHEN a return is requested, THEN the return is refused with a clear reason.
- GIVEN an authorized check-in user has joined the event check-in hub group, WHEN a ticket is checked in, THEN the updated checked-in and issued counts are broadcast.
- GIVEN live sales inventory changes, WHEN organizer/staff clients receive the realtime payload, THEN each ticket type includes sold-out and low-stock flags.

## Verification

- `dotnet test tests/Api.IntegrationTests/EventHub.Api.IntegrationTests.csproj -c Release`
- `yarn --cwd web api:verify`
- `yarn --cwd web build`
