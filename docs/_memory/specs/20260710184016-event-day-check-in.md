---
title: Event-Day Check-in
type: spec
status: implemented
updated_at: 2026-07-10
feature_ids:
  - F-8.1
  - F-8.2
  - F-8.3
  - F-8.4
epic: EP-8
created: 2026-07-10
---

# Event-Day Check-in

Status: implemented

## Problem and solution

Organizers need a trustworthy way to admit attendees at the door using tickets issued after purchase. EventHub must validate a ticket code for the selected event, admit it exactly once, and give staff enough context to handle common door situations without exposing event operations to unauthorized users.

The solution is an authenticated check-in surface for users with the event Check-in permission. Staff can scan or enter a ticket code, search issued tickets by code or buyer email, manually check in a ticket from the search result, and view a simple checked-in versus issued count for the event.

## Acceptance criteria

- Given I hold the Owner or Staff role with Check-in permission for the event, when I scan a valid unused ticket for this event, then it is accepted and marked checked in.
- Given I do not hold Check-in permission for the event, when I attempt scan, manual lookup, manual check-in, or door-count access, then I receive an insufficient permissions response and no check-in state changes.
- Given a code for a different event, an unknown code, a cancelled order, or another invalid ticket state, when I scan or check in that code for this event, then it is rejected with a clear reason.
- Given a ticket has already been checked in, when the same ticket is scanned or manually checked in again, then it is rejected with an already checked in message that includes the first check-in time.
- Given I hold Check-in permission, when I search by full/partial code or buyer email, then I can see matching issued tickets for that event and check them in manually with the same double-entry protection.
- Given I hold Check-in permission, when I view door counts, then I see checked-in tickets and total issued tickets for the event.

## Domain and business rules

- A ticket code admits exactly once.
- A check-in is valid only for a ticket whose event matches the event being checked at the door.
- A ticket can be checked in only from a valid state. Checked-in, transferred, or void tickets are rejected.
- A ticket originating from an order that is cancelled, expired, refunded, or otherwise not confirmed is rejected.
- The first check-in timestamp is preserved and returned when a duplicate scan occurs.
- Check-in state is authoritative in PostgreSQL and protected by the existing command transaction and optimistic concurrency behavior.

## UI behavior or API contract

- The API exposes event-scoped check-in operations under the selected event.
- Scan and manual check-in return an accepted result with ticket identity, holder summary, status, and check-in timestamp, or a problem-details error with a stable code and clear message.
- Manual lookup returns event-scoped ticket summaries searchable by code or buyer email.
- Door counts return checked-in and total-issued counts.
- Browser UI is outside this implementation slice unless generated client refresh is required by the OpenAPI update.

## Data

- Existing ticket fields supply event, order, holder, status, code, issued time, checked-in time, and row version.
- Order status is consulted to reject tickets whose source order is not confirmed.
- No new binary assets, cache-only state, or topology resources are required.

## Real-time

No live push is included in this slice. EP-11 can later subscribe to the check-in data produced here.

## Security

- All check-in operations require an authenticated user with Check-in permission for the target event.
- Authorization is enforced in Application handlers, not only at the HTTP endpoint.
- Responses do not expose domain entities directly.

## Edge cases

- Unknown code: reject with a clear unknown ticket reason.
- Code belongs to another event: reject with a clear wrong event reason.
- Code belongs to an unconfirmed or cancelled order: reject with a clear invalid order reason.
- Already checked in: reject with first check-in time.
- Concurrent scans of the same ticket: only one succeeds; the other receives either an already checked in or retry-exhausted conflict response from existing optimistic concurrency handling.

## Dependencies

- EP-7 ticket issuance and code delivery must exist.
- F-1.7 event-operation authorization must exist.
- F-2.4 published events and F-2.5 cancelled orders/events remain source behaviors this slice reads.

## Assumptions

- QR scan input resolves to the ticket code payload currently issued by EP-7.
- Staff have already been assigned event-scoped roles through existing RBAC flows.
- Cancelled order invalidation may be enforced by consulting order status if issued tickets have not yet been voided by a separate cancellation workflow.

## Out of scope

- Offline-tolerant scanning.
- Dedicated browser check-in UI.
- Live cross-device check-in updates.
- Ticket transfer or return-to-pool behavior.
- New payment/refund behavior.

## Adjacent Feature Boundary

Neighboring features: F-1.5, F-1.7, F-2.5, F-7.1, F-7.2, F-7.3, F-8.5, F-8.6, EP-9, EP-11.

In this slice: MVP event-day check-in API behavior for scan, duplicate prevention, manual lookup/check-in, authorization, and door counts.

Out of scope: multi-device realtime UX guarantees beyond database consistency, offline reconciliation, reporting dashboards, attendee-list exports, and browser scanner UI.

## 7. Harness Impact

- `harness/evals/`: N/A - product slice only; no harness behavior changes.
- `harness/orchestrator/`: N/A - product slice only; no harness behavior changes.
- `.codex/policies/` or `harness/policies/`: N/A - product slice only; no harness behavior changes.
- `harness/telemetry/`: N/A - product slice only; no harness behavior changes.
- `harness/tools/`: N/A - product slice only; no harness behavior changes.
- Workflow surfaces (`harness/graph/`, `.agents/skills/`, `.codex/hooks/`, `scripts/agent/`, `AGENTS.md`): N/A - product slice only; no harness behavior changes.
