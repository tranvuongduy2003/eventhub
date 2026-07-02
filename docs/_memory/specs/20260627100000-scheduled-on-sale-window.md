---
artifact_type: spec
artifact_version: 1
id: spec-20260627100000-scheduled-on-sale-window
title: Scheduled On-Sale Window
slug: scheduled-on-sale-window
filename_template: 20260627100000-scheduled-on-sale-window.md
created_at: 2026-06-27T10:00:00Z
updated_at: 2026-06-27T10:00:00Z
status: draft
owner: product
tags: [spec, eventhub, ticketing-pricing]
feature_refs: [F-3.8]
ddd_refs: [BC-2, AGG-Event, ENT-TicketType, VO-SalesWindow, INV-14]
prd_refs: [QG-1, QG-2, QG-5]
tech_refs: [Tech §4, Tech §6]
db_refs: [Tech §6]
github_issue: 47
search_index:
  keywords: [sales, window, schedule, on-sale, start, end, timer, availability, ticket, type, organizer]
  bounded_contexts: [Event Management]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #47 (https://github.com/tranvuongduy2003/eventhub/issues/47)

# Feature: Scheduled On-Sale Window

> Features: F-3.8  |  Status: DRAFT  |  Date: 2026-06-27
> PRD: QG-1 (simplicity), QG-2 (transparency), QG-5 (correctness at small scale)
> DDD: BC-2 Event Management, AGG-Event, ENT-TicketType, VO-SalesWindow, INV-14
> Tech: §4 (CQRS pipeline), §6 (persistence, concurrency)

## 1. Problem & Solution

**Problem:** Organizers cannot schedule when a ticket type goes on sale or stops selling. Today, once an event is published, all its ticket types are immediately purchasable until manually closed. This forces organizers to either publish at the exact right moment (risking mistakes) or manually toggle ticket types on and off — both fragile and error-prone. Early-bird pricing, timed releases, and last-minute cutoffs are impossible without this capability.

**Solution:** Let the organizer set an optional sales window (start date-time and end date-time) on each ticket type. Before the start time, the ticket type is not purchasable; between start and end, it is; after the end, it stops selling. The event page reflects the current state so attendees see accurate availability. The window is per ticket type, not per event, so different tiers can go on sale at different times (e.g., early-bird before general admission).

**Personas:** PER-O1 (individual organizer) and PER-O2 (small group/club organizer) configure sales windows. PER-A1 and PER-A2 (attendees) experience the resulting availability on the event page.

**Scope:**
- **In:** Set optional start and end date-times on a ticket type; enforce the window during reservation; reflect window state on the event page; owner-only management; edit/remove the window.
- **Out:** Countdown timers on the UI, automated notifications when a window opens/closes, per-event (vs per-ticket-type) sales windows, recurring schedules.

## 2. Acceptance Criteria

**AC-01:** GIVEN I hold the Owner role for an event (F-1.5), WHEN I set a sales window with a start and end date-time on a ticket type, THEN the window is saved and enforced — the ticket type is only purchasable between those times.

**AC-02:** GIVEN a ticket type with a sales window whose start time is in the future, WHEN an attendee views the event page, THEN the ticket type is shown as not yet on sale (with an indication of when sales begin) and cannot be added to an order.

**AC-03:** GIVEN a ticket type with a sales window that is currently open (now is between start and end), WHEN an attendee views the event page, THEN the ticket type is purchasable and behaves as it does today.

**AC-04:** GIVEN a ticket type with a sales window whose end time has passed, WHEN an attendee views the event page, THEN the ticket type shows as sales ended and cannot be added to an order.

**AC-05:** GIVEN a ticket type with no sales window set, WHEN an attendee views the event page, THEN the ticket type is purchasable whenever the event is published (unchanged behavior).

**AC-06:** GIVEN I hold the Owner role, WHEN I attempt to reserve a ticket type whose sales window is not currently open, THEN the reservation is rejected with a clear message indicating the window status (not yet started or already ended).

**AC-07:** GIVEN I hold the Owner role, WHEN I edit the sales window of a ticket type (change start/end or remove it), THEN the change takes effect immediately — future reservations respect the updated window.

**AC-08:** GIVEN I do not hold the Owner role for an event, WHEN I attempt to set or modify a sales window on a ticket type, THEN I am refused with an "insufficient permissions" message.

**AC-09:** GIVEN a ticket type with a sales window and existing confirmed orders, WHEN the end time passes, THEN already-placed orders and their tickets are unaffected — only new reservations are blocked.

## 3. Domain & Business Rules

**Value object: `VO-SalesWindow`** (defined in `domain-model-specification.md`)
- Contains a start date-time and an end date-time, both in UTC.
- End must be after start.
- Optional — a ticket type without a sales window is always on sale (when the event is published).
- Immutable once created; to change, replace the entire value object.

**Invariant INV-14** (`domain-model-specification.md`): A reservation can only succeed when the event is `Published`, the ticket type's sales window is currently open (or absent), and the event is not `Closed` or `Cancelled`. This invariant is the enforcement point for the sales window.

**Behavior on `ENT-TicketType`:**
- Setting a sales window: the organizer provides start and end; the value object is attached to the ticket type.
- The `Reserve` behavior on `AGG-Event` already checks INV-14; the sales window check is an addition to that guard.
- Removing a sales window returns the ticket type to always-on-sale behavior.

**No domain events needed:** The sales window is a configuration attribute on an existing entity, not a state transition that other contexts need to react to.

## 4. UI Behavior

**Organizer — ticket type management (dashboard):**
- When creating or editing a ticket type, the organizer can optionally set a sales window by providing a start date-time and an end date-time (both with time zone, converted to UTC for storage).
- Both fields are optional; leaving both empty means always on sale.
- Validation: end must be after start; both must be in the future when first set (editing an existing window with past dates is allowed if the organizer is adjusting).
- A clear summary shows the current window state: "Sales open Jun 28, 10:00 – Jun 30, 18:00" or "Always on sale."

**Attendee — event page (public):**
- Each ticket type shows its availability state alongside its price:
  - **On sale:** purchasable, same as today.
  - **Not yet on sale:** shown with a note such as "Sales begin Jun 28, 10:00" — not purchasable.
  - **Sales ended:** shown with a note such as "Sales ended" — not purchasable.
  - **No window set:** always on sale (when event is published).
- The state reflects the current time at page load; no real-time countdown is required.

## 5. Data & Storage Impact

**PostgreSQL (`app` schema):**
- The `TicketType` table gains two nullable columns: `sales_window_start` (`timestamptz`) and `sales_window_end` (`timestamptz`).
- Both nullable = no window set (always on sale). Both non-null = window defined.
- No new tables required; the sales window is an attribute of the existing ticket type entity.
- Migration is additive (new nullable columns) — backward-compatible.

**Redis / MinIO / RabbitMQ:** No impact. The sales window is a domain attribute checked at reservation time; no caching, storage, or messaging changes required.

## 6. Real-Time & Consistency

**Consistency:** The sales window check happens at reservation time inside the `Event` aggregate (INV-14). Since reservations already use optimistic concurrency, the window check is consistent with the existing no-oversell mechanism.

**Real-time:** No SignalR push is required for this feature. The event page reflects the window state on each page load. A future enhancement (F-11.1) could push live availability updates, but that is out of scope here.

**Edge timing:** If an attendee loads the page while a window is open, then sits on the page past the end time, the reservation attempt will fail (INV-14) with a clear message. This is acceptable behavior — the page is not live-refreshing.

## 7. Security & Privacy

- Only users with the Owner role (F-1.5) can set or modify sales windows. Enforcement is in the Application handler via `ICurrentUserAccessor`.
- The sales window is a business configuration, not personal data. No privacy concerns.
- No payment boundary interaction — the window controls whether a reservation can start, not how payment proceeds.

## 8. Edge Cases

**EC-01:** A ticket type has a sales window with start = end (zero-duration window). The window is effectively never open. This is valid but useless; the system accepts it without error. The ticket type shows as "not yet on sale" indefinitely.

**EC-02:** An attendee loads the event page during a sales window, then attempts to reserve after the window closes. The reservation fails with a clear message ("Sales for this ticket type have ended").

**EC-03:** An organizer changes the sales window while attendees are mid-checkout. The reservation check uses the current window at the moment of reservation, not at page load. This is correct behavior.

**EC-04:** A ticket type has a sales window in the past (both start and end are before now). The ticket type shows as "Sales ended" and is not purchasable. The organizer can edit the window to extend it.

**EC-05:** An event has two ticket types — one with a sales window and one without. The one without is always on sale; the one with a window follows its schedule. They are independent.

**EC-06:** The organizer sets a sales window on a ticket type that already has a reservation hold (pending order). The existing hold is unaffected (AC-09); only new reservations respect the updated window.

## 9. Dependencies & Risks

**Dependencies:**
- F-3.1 (Define a ticket type) — the ticket type must exist before a sales window can be set on it. Already implemented.
- F-1.5 (Define roles and permissions) — Owner role check. Already implemented.
- F-3.4 (Inventory and no-oversell) — reservation mechanism that the window check hooks into. Already implemented.

**Risks:**
- **Time zone confusion:** Organizers may think in local time but the system stores UTC. Mitigation: the UI accepts local time with time zone and converts; display always shows in the organizer's configured time zone.
- **Clock skew:** If the server clock is slightly off, the window boundary could be a few seconds early or late. Acceptable for this scale (QG-5 — small events, modest demand).

## 10. Assumptions

- The organizer's time zone is known (from the event's schedule configuration, `VO-EventSchedule`).
- Sales windows are per ticket type, not per event. An event-level window could be added later but is not in scope.
- No automated notification to attendees when a sales window opens. The organizer can share the timing via their own channels.

## 11. Out of Scope

- Countdown timers or real-time UI updates for window transitions.
- Automated email/notification when a sales window opens or closes.
- Per-event (vs per-ticket-type) sales windows.
- Recurring or repeating sales schedules.
- Analytics on sales window performance (deferred to EP-9).

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the system prevent an organizer from setting a sales window start in the past, or allow it (effectively making the ticket type immediately on sale)? Current design allows it. | ✅ Resolved — allow past start (organizer may be adjusting an existing window). |
