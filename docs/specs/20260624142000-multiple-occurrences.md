---
artifact_type: spec
artifact_version: 1
id: spec-20260624142000-multiple-occurrences
title: Multiple Occurrences / Sessions
slug: multiple-occurrences
filename_template: 20260624142000-multiple-occurrences.md
created_at: 2026-06-24T14:20:00+07:00
updated_at: 2026-06-24T14:20:00+07:00
status: draft
owner: product
tags: [spec, eventhub, event-management]
feature_refs: [F-2.7]
ddd_refs: [BC-2, AGG-Event, INV-10, INV-11, INV-12, INV-14]
prd_refs: [QG-1, QG-5]
tech_refs: [Tech §6]
db_refs: [Tech §6]
github_issue: 1
search_index:
  keywords: [occurrence, session, recurring, multi-date, inventory, event, schedule, ticket, capacity]
  bounded_contexts: [BC-2]
  user_personas: [PER-O2]
---

> GitHub: #1 (https://github.com/tranvuongduy2003/EventHub/issues/1)

# Feature: Multiple Occurrences / Sessions

> Features: F-2.7  |  Status: DRAFT  |  Date: 2026-06-24
> PRD: QG-1 (simplicity), QG-5 (correctness)  |  DDD: BC-2, AGG-Event  |  Tech: §6

## 1. Problem & Solution

**Problem:** Some events run on multiple dates — a weekly workshop, a multi-day class, a recurring meetup. Today, an organizer must create a separate event for each date, which fragments their page, their attendee data, and their setup effort. There is no way to present "same event, different dates" as one coherent offering.

**Solution:** Allow an organizer to add one or more **occurrences** to an event. Each occurrence has its own date, its own ticket inventory, and its own lifecycle — but they all share the event's title, description, cover image, and public page. Attendees see a single event page and choose which occurrence to attend.

**Personas:** PER-O2 (small group / club / small-business organizer) — needs to offer repeating or multi-session events without creating duplicate events.

**Scope:**
- **In:** F-2.7 only — add/remove/edit occurrences on an event; per-occurrence inventory; public page shows occurrences; attendee selects an occurrence at checkout; check-in validates against a specific occurrence.
- **Out:** F-2.1 draft creation (already done); F-2.2 cover image; F-2.3 edit details; F-2.4 publish; F-2.5 close/cancel; F-2.6 duplicate; F-3.1 ticket type creation (ticket types are shared across occurrences; inventory is per-occurrence); F-4.3 rich link previews; F-11.* realtime updates for per-occurrence counts.

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in and hold the Owner role for an event (F-1.5), WHEN I add an occurrence with a date-time that does not overlap an existing occurrence for the same event, THEN the occurrence is created with its own inventory pool (initialized at the same capacity as the event's ticket types) and appears in the event's occurrence list.

**AC-02:** GIVEN I hold the Owner role for an event, WHEN I add an occurrence with a date-time that overlaps an existing occurrence for the same event (same start or overlapping time range), THEN the addition is rejected with a clear "occurrence overlaps" message.

**AC-03:** GIVEN I hold the Owner role for an event, WHEN I edit an occurrence's date-time, THEN the change is saved and reflected on the public page, provided the new date does not overlap another occurrence.

**AC-04:** GIVEN I hold the Owner role for an event, WHEN I remove an occurrence that has zero sold and zero reserved tickets, THEN the occurrence is removed and its inventory pool is deleted.

**AC-05:** GIVEN I hold the Owner role for an event, WHEN I attempt to remove an occurrence that has sold or reserved tickets, THEN the removal is blocked with a clear message explaining that tickets have been issued or reserved for this occurrence.

**AC-06:** GIVEN I hold the Owner role for an event, WHEN I change a ticket type's capacity, THEN each occurrence's inventory pool adjusts proportionally or is set to the new capacity, provided no occurrence's sold + reserved exceeds the new capacity.

**AC-07:** GIVEN a published event with multiple occurrences, WHEN any visitor opens the public event page, THEN they see all future occurrences listed with their dates and per-occurrence availability, alongside the shared event details (title, description, cover, location).

**AC-08:** GIVEN a published event with multiple occurrences, WHEN an attendee selects an occurrence and proceeds to checkout, THEN the order is linked to that specific occurrence and inventory is reserved from that occurrence's pool only.

**AC-09:** GIVEN a ticket issued for a specific occurrence, WHEN staff scan the ticket at check-in (F-8.1), THEN the ticket is validated against the occurrence it was issued for. A ticket for a different occurrence of the same event is rejected with a clear "wrong occurrence" message.

**AC-10:** GIVEN I hold the Owner role for an event with multiple occurrences, WHEN I view the organizer dashboard, THEN I see per-occurrence stats: tickets sold, availability, and check-in counts for each occurrence.

**AC-11:** GIVEN I hold the Owner role for an event, WHEN I close or cancel the event (F-2.5), THEN the close/cancel applies to all occurrences. Individual occurrence close/cancel is not supported.

**AC-12:** GIVEN an event with multiple occurrences, WHEN I duplicate the event (F-2.6), THEN the duplicated event copies the occurrence structure but not any sales, attendees, or inventory data. The organizer must set new dates for each occurrence in the duplicate.

**AC-13:** A user without the Owner role is refused with an "insufficient permissions" message when attempting to add, edit, or remove occurrences.

## 3. Domain & Business Rules

**Aggregate alignment (BC-2 — Event Management):**

The `Event` aggregate currently owns a single `VO-EventSchedule`. With F-2.7, occurrences become **child entities** within the `Event` aggregate, because they share the event's consistency boundary and the no-oversell invariant (`INV-10`) must hold per occurrence.

**New entity:** `ENT-Occurrence` — a child of `AGG-Event`.
- Identity: `VO-OccurrenceId`
- Attributes: `VO-EventSchedule` (date-time range), per-ticket-type inventory counters (Sold, Reserved — capacity is inherited from the ticket type definition but tracked per occurrence).
- Each occurrence maintains its own `Reserved + Sold ≤ Capacity` invariant.

**Invariants (new or modified):**
- **INV-10 (modified):** `Reserved + Sold ≤ Capacity` per ticket type **per occurrence**. The no-oversell guarantee is per-occurrence, not per-event.
- **INV-26 (new):** An event's occurrences must not have overlapping time ranges (same start, or start < other end and end > other start).
- **INV-27 (new):** An occurrence with `Sold + Reserved > 0` cannot be removed.
- **INV-28 (new):** Ticket type capacity changes must not cause any occurrence's `Sold + Reserved` to exceed the new capacity.

**Lifecycle:**
- Occurrences are created in `Scheduled` status.
- Closing/cancelling the event (F-2.5) cascades to all occurrences.
- Individual occurrence close/cancel is not supported; the event-level action is the only way.

**Domain events (new):**
- `EVT-OccurrenceAdded` — when an organizer adds an occurrence.
- `EVT-OccurrenceUpdated` — when an occurrence's date changes.
- `EVT-OccurrenceRemoved` — when an occurrence is deleted.

**Ticket type relationship:**
- Ticket types remain defined at the **event level** (shared name, price, description).
- Inventory (capacity, sold, reserved) is tracked **per occurrence per ticket type**.
- This avoids duplicating ticket type definitions while allowing independent inventory control.

**Interaction with existing features:**
- **F-2.4 Publish:** An event can be published if it has ≥1 ticket type **and** ≥1 occurrence.
- **F-2.5 Close/Cancel:** Closing/cancelling the event cascades to all occurrences. Individual occurrence close/cancel is not supported.
- **F-2.6 Duplicate:** Copies the occurrence structure. Organizer must set new dates for each occurrence.
- **F-3.1 Ticket types:** Defined at event level; inventory tracked per occurrence.
- **F-3.4 Inventory:** Per-occurrence no-oversell.
- **F-5.1–F-5.5 Checkout:** Attendee selects an occurrence; reservation is against that occurrence's inventory.
- **F-8.1 Check-in:** Ticket validates against its occurrence.

## 4. UI Behavior

**Organizer — Event edit page:**
- New "Occurrences" section in the event edit view.
- List of existing occurrences with date, status, and inventory summary.
- "Add Occurrence" action: date-time picker (with time zone inherited from event).
- Edit/remove actions per occurrence (remove blocked if inventory consumed).

**Organizer — Dashboard:**
- Per-occurrence breakdown of sold, available, and check-in counts.
- Occurrence-level filtering on attendee list.

**Attendee — Public event page:**
- Occurrence selector (dropdown or list) showing upcoming occurrences with date and availability.
- Selecting an occurrence updates the displayed availability for ticket types.
- "Get Tickets" flow is scoped to the selected occurrence.

**Attendee — Checkout:**
- Selected occurrence is shown in the order summary.
- Reservation and payment are scoped to that occurrence.

**Attendee — Ticket/QR:**
- Ticket displays the occurrence date alongside the event title.

## 5. Data & Storage Impact

**PostgreSQL:**
- New `occurrences` table under `app` schema, owned by the `event` aggregate.
  - Columns: `id`, `event_id` (FK), `starts_at`, `ends_at`, `time_zone`, `created_at`, `updated_at`.
- New `occurrence_ticket_type_inventory` table (or similar join) to track per-occurrence, per-ticket-type inventory.
  - Columns: `occurrence_id`, `ticket_type_id`, `capacity`, `sold`, `reserved`.
- Foreign keys: `occurrences.event_id → events.id`, inventory table references both.
- Optimistic concurrency (`row_version`) on the `occurrences` table or on the inventory rows to protect the no-oversell invariant per occurrence.

**Impact on existing tables:**
- `orders` table: add `occurrence_id` FK (nullable for backward compatibility with existing orders that predate occurrences).
- `tickets` table: add `occurrence_id` FK.
- `reservations` table: add `occurrence_id` FK.

**Redis:** No new cache concerns; existing cache invalidation patterns apply.

**MinIO / RabbitMQ:** No changes.

## 6. Real-Time & Consistency

**Consistency:**
- Strong consistency within the `Event` aggregate: adding/removing occurrences and reserving/releasing inventory per occurrence happen in the same transaction.
- The no-oversell invariant (`INV-10`) is enforced per-occurrence with optimistic concurrency retry.

**Real-time (future — F-11.*):**
- When F-11.1 (live sales) is implemented, it should support per-occurrence counts. This is out of scope for this spec but the data model supports it.

**Integration events:**
- `EVT-OccurrenceAdded`, `EVT-OccurrenceUpdated`, `EVT-OccurrenceRemoved` are published for downstream consumers (Reporting, Notifications).
- Idempotent consumption per existing patterns.

## 7. Security & Privacy

- **Authorization:** All occurrence management operations require the Owner role on the event (F-1.5). Staff role has no occurrence management permissions.
- **Public access:** Occurrence list and per-occurrence availability are visible on the public event page (same as event details today). No new private data exposed.
- **Guest checkout:** Unchanged — attendee provides name + email, order is scoped to a specific occurrence.
- **No new data collection:** Occurrences add date/schedule data only; no personal data changes.

## 8. Edge Cases

**EC-01:** An organizer adds an occurrence to a Published event that already has sold tickets for other occurrences. → Allowed; the new occurrence starts with full inventory.

**EC-02:** An organizer tries to reduce a ticket type's capacity below the total sold + reserved across all occurrences. → Blocked with a message showing which occurrences would be affected.

**EC-03:** An attendee has a ticket for Occurrence A but tries to check in at Occurrence B (same event, different date). → Rejected with "this ticket is for [Occurrence A date]; this is [Occurrence B date]."

**EC-04:** All occurrences of a Published event are in the past. → The event shows "no upcoming occurrences" on the public page. Organizer should close or cancel the event.

**EC-05:** An organizer removes all occurrences from a Published event. → Blocked if the event is Published (INV-11 requires ≥1 occurrence for publishability). If already Published, must have ≥1 active occurrence or the event should be closed.

**EC-06:** Two occurrences are scheduled at the same time. → Blocked by INV-26 (no overlap).

**EC-07:** An attendee places an order for an occurrence, then the event is cancelled before payment completes. → The order's hold expires normally; the attendee sees the event is no longer available.

**EC-08:** Duplicating an event with occurrences. → The new Draft event copies occurrence structure; the organizer must set new dates for each occurrence before publishing.

## 9. Dependencies & Risks

**Dependencies:**
- F-2.1 (Create draft event) — already implemented; occurrences extend the existing Event aggregate.
- F-1.5 (Roles and permissions) — already implemented; Owner role gates occurrence management.
- F-3.1 (Ticket types) — already implemented; ticket types are defined at event level, inventory is per-occurrence.

**Risks:**
- **Complexity increase (QG-1):** This feature adds a new entity and per-occurrence inventory tracking, increasing the Event aggregate's complexity. Mitigation: keep occurrences as child entities (not a separate aggregate) to maintain strong consistency without cross-aggregate coordination.
- **Migration impact:** Existing events have no occurrences. A migration strategy is needed — existing events get a single "default" occurrence derived from their current schedule, or occurrences are required only for events created after this feature.
- **Performance:** Events with many occurrences (e.g., daily for a year) could make the aggregate large. Mitigation: archive or paginate past occurrences if needed.

## 10. Assumptions

1. Occurrences are **date-based sessions**, not time-slot bookings. Each occurrence is a single event happening at a specific date-time.
2. Ticket types are shared across all occurrences (same name, price, description). Only inventory is per-occurrence.
3. The time zone is set at the event level and inherited by all occurrences.
4. An event can have at most one active (Scheduled) occurrence at any given time (enforced by INV-26).
5. Existing events (pre-F-2.7) continue to work without occurrences. No backfill is needed — occurrences are only for events created after this feature.

## 11. Out of Scope

- Ticket type definitions per occurrence (ticket types remain event-level).
- Attendee ability to switch occurrences after purchase (would require a new feature).
- Per-occurrence pricing variations.
- Occurrence-level cover images or descriptions.
- Automatic occurrence generation from a recurrence rule (e.g., "every Monday for 10 weeks").
- Individual occurrence close/cancel (only event-level close/cancel supported).
- Real-time per-occurrence sales updates (F-11.* — separate feature).

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | How should existing events (pre-F-2.7) be migrated? Should they get a default occurrence, or is the feature only for new events? | ✅ No backfill needed. Existing events continue to work with their implicit single schedule. Occurrences are only for events created after this feature. |
| 2 | Should there be a maximum number of occurrences per event? If so, what limit? | ✅ No limit. |
| 3 | When an event is duplicated (F-2.6), should the organizer be prompted to set new dates for each occurrence, or should dates be cleared entirely? | ✅ Organizer must set new dates for each occurrence when duplicating. |
| 4 | Should closing/cancelling an individual occurrence trigger attendee notifications (email), or only event-level close/cancel? | ✅ Only event-level close/cancel. Individual occurrence close/cancel is out of scope. |
