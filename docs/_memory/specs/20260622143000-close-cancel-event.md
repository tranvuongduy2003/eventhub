---
artifact_type: spec
artifact_version: 1
id: spec-20260622143000-close-cancel-event
title: Close or Cancel an Event
slug: close-cancel-event
filename_template: 20260622143000-close-cancel-event.md
created_at: "2026-06-22T14:30:00Z"
updated_at: "2026-06-22T14:30:00Z"
status: draft
owner: product
tags: [spec, eventhub, event-management]
feature_refs: [F-2.5]
ddd_refs: [BC-2, AGG-Event, INV-10, INV-14, EVT-EventClosed, EVT-EventCancelled]
prd_refs: [DEC-1, QG-1]
tech_refs: [Tech §4, Tech §5]
db_refs: [Tech §6]
github_issue: 29
search_index:
  keywords: [close, cancel, event, lifecycle, stop sales, refund, cancellation]
  bounded_contexts: [Event Management, Sales, Payments, Ticketing, Notifications]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #29 (https://github.com/tranvuongduy2003/eventhub/issues/29)

# Feature: Close or Cancel an Event

> Features: F-2.5  |  Status: DRAFT  |  Date: 2026-06-22
> PRD: DEC-1 (no platform fee), QG-1 (simplicity)  |  DDD: BC-2 AGG-Event lifecycle  |  Tech: §4–6

## 1. Problem & Solution

**Problem:** An organizer needs to stop selling tickets for an event — either temporarily (sales are done, but the event still happens) or permanently (the event is off). Without this, a published event stays sellable forever, even when it should not be.

**Solution:** Two distinct actions on a published event:
- **Close** — stops all new purchases; issued tickets remain valid for entry. The event is still happening, just not accepting new buyers.
- **Cancel** — the event is not happening. Sales stop, the event is marked cancelled, and once the payments epic (EP-6) exists, paid orders are refunded and tickets are voided. Attendees can be notified.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group organizer).

**Scope:** F-2.5 only. Refund-on-cancellation (F-6.6) and attendee notification (F-9.5) are separate features referenced as dependencies but not implemented in this slice. The Cancel action records the intent and marks the event; downstream effects are wired when those features land.

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in and hold the Owner role for a Published event, WHEN I Close the event, THEN the event status changes to Closed and no new purchases can be started for it.

**AC-02:** GIVEN a Closed event, WHEN anyone opens its public page, THEN they see an appropriate "event closed" state and no buy option is available.

**AC-03:** GIVEN a Closed event with previously issued tickets, WHEN a ticket holder arrives at the door, THEN their ticket is still valid for check-in (closing does not invalidate existing tickets).

**AC-04:** GIVEN I am signed in and hold the Owner role for a Published or Closed event, WHEN I Cancel the event, THEN the event status changes to Cancelled and no new purchases can be started.

**AC-05:** GIVEN a Cancelled event, WHEN anyone opens its public page, THEN they see an appropriate "event cancelled" state and no buy option is available.

**AC-06:** GIVEN I do not hold the Owner role for the event, WHEN I attempt to Close or Cancel, THEN I am refused with an "insufficient permissions" message and no status change occurs.

**AC-07:** GIVEN a Draft event (not yet published), WHEN I attempt to Close or Cancel, THEN the action is rejected — only Published or Closed events can be Closed or Cancelled respectively.

**AC-08:** GIVEN I hold the Owner role for a Published event, WHEN I Cancel it, THEN the system records the cancellation so that downstream features (F-6.6 refunds, F-9.5 notifications) can act on it when they are built.

**AC-09:** GIVEN an already Closed event, WHEN I Cancel it, THEN the event moves from Closed to Cancelled (Cancel supersedes Close).

**AC-10:** GIVEN an already Cancelled event, WHEN I attempt to Close or Cancel again, THEN the action is rejected — Cancelled is a terminal state.

## 3. Domain & Business Rules

**Lifecycle transitions (from domain-model-specification.md §7):**

| From | Action | To | Guard |
|------|--------|----|-------|
| Published | Close | Closed | Caller holds Owner role |
| Published | Cancel | Cancelled | Caller holds Owner role |
| Closed | Cancel | Cancelled | Caller holds Owner role |

- Close is only valid from **Published**.
- Cancel is valid from **Published** or **Closed**.
- **Cancelled is terminal** — no further status transitions.
- **Draft** events cannot be Closed or Cancelled (they are not yet public; the organizer can simply stop editing or delete them — deletion is out of scope for this spec).

**Domain events raised:**
- `EVT-EventClosed` — raised when an event is Closed.
- `EVT-EventCancelled` — raised when an event is Cancelled. This is an **integration event** that will fan out to Sales, Payments, Ticketing, and Notifications (per domain-model-specification.md §6) when those consumers are built.

**Inventory impact:**
- **Close:** no inventory change. Existing reservations and sold tickets are untouched. New reservations are blocked by the Closed status (INV-14: cannot reserve unless Published).
- **Cancel:** existing reservations should be released (inventory returns to pool, though the pool is moot since the event is cancelled). Sold tickets are voided. This is downstream work (F-6.6, EP-10) — the domain event signals the cancellation for those features to consume.

**Authorization:**
- Only the **Owner** of an event can Close or Cancel it. This aligns with F-1.5 (Owner = full control) and is enforced in the Application handler, not just the API layer.

## 4. UI Behavior

**Organizer dashboard:**
- For a Published event, the organizer sees a "Close Event" action and a "Cancel Event" action (with confirmation).
- For a Closed event, the organizer sees a "Cancel Event" action (Close is no longer available since it is already closed).
- For a Cancelled event, neither action is available.
- Both actions require a confirmation step (e.g., "Are you sure you want to cancel? This cannot be undone.").

**Public event page:**
- Published → normal page with buy option.
- Closed → page still accessible but shows "This event is closed for registration" or similar; no buy button; issued tickets still work.
- Cancelled → page shows "This event has been cancelled"; no buy button.

**Confirmation dialog for Cancel:**
- Clearly states the consequences: sales stop, tickets are invalidated, and (once F-6.6 exists) refunds will be processed.
- Requires explicit confirmation to proceed.

## 5. Data & Storage Impact

**PostgreSQL:**
- The `Event` aggregate's status field transitions as described. No new columns needed — `VO-EventStatus` already includes `Closed` and `Cancelled` states.
- A `CancelledAt` timestamp may be added to the Event record for audit and downstream reference (aligns with Tech §6 timestamp conventions).

**Redis / MinIO / RabbitMQ:**
- No direct storage changes.
- `EVT-EventCancelled` will be published to RabbitMQ when downstream consumers (F-6.6, F-9.5) are built. For this slice, the domain event is raised in-process and the integration event infrastructure is prepared but consumers are not yet active.

## 6. Real-Time & Consistency

- **SignalR (EP-11):** When the realtime epic is built, a Close or Cancel should push a live update to any open organizer dashboard viewing that event. For this slice, no SignalR integration is required.
- **Consistency:** The status transition is a single-aggregate write (strong consistency within the Event aggregate, protected by optimistic concurrency). Downstream effects (refunds, notifications, ticket voiding) are eventual, driven by integration events.

## 7. Security & Privacy

- **Authorization:** Close and Cancel require the Owner role, enforced in the Application handler via `ICurrentUserAccessor` (Constitution II.7). The API layer also checks but is not the sole guard.
- **Session:** Only authenticated organizers with the Owner role can perform these actions. Guest attendees cannot Close or Cancel.
- **No sensitive data exposed:** Close/Cancel do not expose attendee PII. The public page simply shows a status message.

## 8. Edge Cases

**EC-01:** Organizer tries to Close an already Closed event → rejected (idempotency: already Closed, no change).

**EC-02:** Organizer tries to Cancel an already Cancelled event → rejected (terminal state).

**EC-03:** Organizer tries to Close a Cancelled event → rejected (Cancel is terminal; Close is not a valid transition from Cancelled).

**EC-04:** Two administrators try to Cancel the same event simultaneously → optimistic concurrency ensures only one succeeds; the other gets a conflict error and can retry (the event is already Cancelled).

**EC-05:** Event has pending (unpaid) orders when Cancelled → pending orders should eventually expire or be cancelled. This is handled by the hold-expiry mechanism (F-5.5) and the `EVT-EventCancelled` integration event consumed by Sales. For this slice, the domain event is raised; order cancellation is downstream.

**EC-06:** Event has confirmed (paid) orders when Cancelled → refunds are triggered by F-6.6 (not in this slice). The cancellation is recorded so F-6.6 can act on it.

**EC-07:** Organizer is mid-edit when another owner (if ownership was just transferred) Cancelled the event → the edit should fail with a conflict or status error, since the event is no longer in an editable state.

## 9. Dependencies & Risks

**Upstream dependencies (already built):**
- F-1.2 (Sign in) — organizer must be authenticated.
- F-1.5 / F-1.6 (Roles) — Owner role must exist and be assignable.
- F-2.1 (Create draft) — event must exist.
- F-2.4 (Publish) — event must be Published before it can be Closed or Cancelled.

**Downstream dependencies (not in this slice):**
- F-6.6 (Refund on cancellation) — consumes `EVT-EventCancelled` to trigger refunds.
- F-9.5 (Light messaging) — consumes `EVT-EventCancelled` to notify attendees.
- EP-10 (Transfer & Returns) — Cancel voids tickets; transfer/return rules interact with Cancelled state.

**Risks:**
- **Scope creep into refunds:** The Cancel action is meaningful only when refunds eventually happen. This slice must clearly record the cancellation intent without implementing the refund flow. Risk of partial implementation confusing users if Cancel is visible but refunds are not yet built.
- **Downstream consumer readiness:** `EVT-EventCancelled` integration event should be published even if no consumers exist yet, to avoid a later migration. Consumers (Sales, Payments, Ticketing, Notifications) should handle the event idempotently when they are built.

## 10. Assumptions

- The `VO-EventStatus` value object already supports `Closed` and `Cancelled` states (as defined in domain-model-specification.md).
- The Event aggregate already has `Close` and `Cancel` behaviors defined (as listed in domain-model-specification.md §4 BC-2).
- Owner role assignment (F-1.6) is functional.
- The public event page (F-4.1) can render different states (Draft, Published, Closed, Cancelled).
- For this slice, Cancel marks the event and raises the domain event. Actual refund processing (F-6.6) and attendee notification (F-9.5) are separate features.

## 11. Out of Scope

- **Refund processing (F-6.6):** triggered by `EVT-EventCancelled` but implemented separately.
- **Attendee notification on cancellation (F-9.5):** email delivery is a separate feature.
- **Ticket voiding on cancellation:** downstream effect handled by Ticketing context when F-6.6 / EP-10 are built.
- **Event deletion:** removing a Draft event entirely is not covered by this feature.
- **Partial cancellation:** cancelling specific ticket types or sessions; not a concept in the current model.
- **Real-time push of status changes (EP-11):** will consume `EVT-EventClosed` / `EVT-EventCancelled` when built.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should Cancel on an event with confirmed orders be blocked until F-6.6 (refunds) is built, or should it proceed and record the intent? | ✅ Resolved: proceed and record — downstream features consume the event when ready. |
| 2 | Should a `CancelledAt` timestamp be added to the Event aggregate for audit purposes? | ✅ Resolved: yes, lightweight addition aligned with Tech §6 conventions. |
| 3 | Should the public page for a Closed event differ visually from Cancelled (e.g., "sales closed" vs "event cancelled")? | ✅ Resolved: yes, distinct messaging for clarity. |
