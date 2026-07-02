---
artifact_type: spec
artifact_version: 1
id: spec-20260621180000-edit-event-details
title: Edit event details
slug: edit-event-details
filename_template: 20260621180000-edit-event-details.md
created_at: 2026-06-21T18:00:00+07:00
updated_at: 2026-06-21T18:00:00+07:00
status: draft
owner: product
tags: [spec, eventhub, event-creation-management]
feature_refs: [F-2.3]
ddd_refs: [BC-2, AGG-Event, INV-10, INV-12, INV-13]
prd_refs: [DEC-3, QG-1, QG-5, PER-O1, PER-O2]
tech_refs: [Tech §5, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 25
search_index:
  keywords:
    - edit event
    - update event
    - event details
    - event management
    - draft event
    - published event
    - capacity guard
    - organizer
    - event lifecycle
  bounded_contexts: [BC-2 Event Management]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: [#25](https://github.com/tranvuongduy2003/eventhub/issues/25)

# Feature: Edit event details

> Features: F-2.3  |  Status: DRAFT  |  Date: 2026-06-21
> PRD: DEC-3 (MVP spine), QG-1 (simplicity), QG-5 (correctness)  |  DDD: BC-2 · AGG-Event · INV-12  |  Tech: §5–7

## 1. Problem & Solution

**Problem:** Once an event is created (F-2.1), the organizer needs to refine its details — fix typos, adjust the schedule, update the description, or change the location. Without editing, every mistake requires creating a new event from scratch. Editing becomes especially important once the event is published and selling tickets: the organizer must be able to update descriptive information (like the description or venue directions) without breaking the ticket inventory that attendees have already purchased.

**Solution:** The organizer opens an existing event and edits its details. While the event is in **Draft**, all fields are freely editable. While **Published**, descriptive fields (title, description, location, schedule) remain editable, but changes that would harm existing ticket holders — such as reducing a ticket type's capacity below the number already sold or reserved — are blocked with a clear explanation. The system enforces this guard to protect the no-oversell guarantee (INV-10) and the integrity of confirmed purchases.

**Personas:** PER-O1 (individual organizer — wants to quickly fix mistakes), PER-O2 (small group organizer — wants to update event information as plans evolve).

**Scope:**
- **In:** F-2.3 only — edit an existing event's core details (title, description, schedule, location), with status-dependent guards on destructive changes.
- **Out:** F-2.2 cover image upload (separate feature); F-2.4 publish; F-2.5 close/cancel; F-2.6 duplicate; ticket type editing (F-3.1, F-3.5); discount codes (F-3.7); slug changes (slug is set at publish time, INV-15).

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in and I hold the Owner role (F-1.5) for an event in **Draft** status, WHEN I update any event detail (title, description, start/end date-time, time zone, location type, physical address, or online flag), THEN the event is saved with the new values and I receive a success response with the updated event data.

**AC-02:** GIVEN I am signed in and I hold the Owner role for an event in **Published** status, WHEN I update a descriptive field (title, description, location details, schedule), THEN the event is saved with the new values, provided the change does not violate inventory guards (AC-04).

**AC-03:** GIVEN I am signed in and I hold the Owner role for an event in **Published** status, WHEN I attempt to change a ticket type's capacity to a value below the number already sold plus reserved for that ticket type, THEN the edit is rejected with a clear message explaining that the capacity cannot be reduced below the number of tickets already committed (sold + reserved), and the event remains unchanged.

**AC-04:** GIVEN I am signed in but I do not hold the Owner role for the event (e.g., I am Staff or have no role), WHEN I attempt to edit any event detail, THEN I am refused with an "insufficient permissions" message and no changes are made.

**AC-05:** GIVEN I am signed in and I hold the Owner role, WHEN I update the event with invalid data (empty title, end date before start date, missing required location fields), THEN the edit is rejected with a clear validation message indicating which field is invalid, and the event remains unchanged.

**AC-06:** GIVEN I am signed in and I hold the Owner role for an event in **Closed** or **Cancelled** status, WHEN I attempt to edit event details, THEN the edit is rejected with a message explaining that a closed or cancelled event cannot be modified.

**AC-07:** GIVEN I am signed in and I hold the Owner role, WHEN I update the event schedule (start/end date-time or time zone), THEN the new schedule is persisted and the time zone is stored correctly for display to attendees.

## 3. Domain & Business Rules

**Bounded context:** BC-2 — Event Management.

**Aggregate:** AGG-Event. Editing operates through the `UpdateDetails` behavior on the aggregate.

**Invariants enforced:**
- **INV-10 (no-oversell):** `Reserved + Sold ≤ Capacity` per ticket type. When the organizer edits a Published event, any change to capacity must respect this. Reducing capacity below `Reserved + Sold` is rejected.
- **INV-12 (capacity guard):** Capacity cannot drop below `Reserved + Sold`. This is the specific guard that protects published events from destructive edits.
- **INV-13 (price ≥ 0):** If price editing is in scope for a future ticket-type edit flow, prices must remain non-negative. Not directly triggered by this feature (ticket type editing is F-3.1/F-3.5), but the invariant exists on the aggregate.

**Status-dependent behavior:**
- **Draft:** `UpdateDetails` applies freely — all fields mutable, no guards beyond basic validation (non-empty title, valid schedule, valid location).
- **Published:** `UpdateDetails` applies to descriptive fields; inventory-related fields (ticket type capacity) are guarded by INV-12. The aggregate rejects changes that would violate the guard.
- **Closed / Cancelled:** `UpdateDetails` is rejected — terminal states are immutable (per the lifecycle in domain-model-specification.md §7).

**Authorization:** Ownership is checked before the aggregate is loaded. Only the user holding the Owner role for that event may invoke `UpdateDetails`. This aligns with F-1.5 and the Application-layer authorization pattern (Constitution II.7).

**No domain events:** Editing event details is a routine mutation. It does not raise a domain event unless a downstream consumer needs to react (e.g., Reporting projections may need to update, but that is handled via the existing `EventPublished` or a future lightweight update event — out of scope for this MVP feature).

## 4. UI Behavior

**Screen:** The organizer opens an event they own from their dashboard (F-9.4, when available) or directly. The event detail/edit form displays the current values of all editable fields.

**Draft events:**
- All fields are editable: title, description, start date-time, end date-time, time zone, location type (physical/online), physical address.
- A "Save" button submits changes. On success, the form reflects the updated values.
- On validation failure, inline error messages appear next to the relevant fields.

**Published events:**
- Descriptive fields remain editable (title, description, location, schedule).
- Ticket type capacity fields, if shown on this form, are editable but guarded: if the organizer enters a value below sold + reserved, the save is rejected with an inline message: *"Capacity cannot be reduced below [number] — [number] tickets are already sold or reserved."*
- The form clearly indicates the event is Published, so the organizer understands they are editing a live event.

**Closed / Cancelled events:**
- The edit form is read-only or shows a banner: *"This event is closed/cancelled and cannot be edited."*

**Permission denied:**
- A user without the Owner role who navigates to the edit form sees an "insufficient permissions" message and cannot make changes.

**Mobile:** The edit form is usable on a phone (QG-4) — single-column layout, full-width inputs, no horizontal scrolling.

## 5. Data & Storage Impact

**PostgreSQL (app schema):**
- The `Event` table's mutable columns (title, description, schedule start/end, time zone, location type, address) are updated in place.
- No schema changes required — the columns already exist from F-2.1.
- Optimistic concurrency via `row_version` on the `Event` aggregate root prevents lost updates when two editors save simultaneously (Tech §6).

**Redis:** No direct impact. If event data is cached (e.g., for the public page in EP-4), the cache is invalidated or overwritten on the next read. Cache is rebuildable (Constitution V).

**MinIO:** No impact — cover image editing is F-2.2, separate from this feature.

**RabbitMQ:** No integration events emitted for a simple detail edit (see §3 — no domain events for routine mutations).

## 6. Real-Time & Consistency

**N/A for this feature.** Editing event details does not trigger real-time push to attendees or staff. If a published event's details change, attendees who have already loaded the page will see stale data until they refresh — this is acceptable for MVP (eventual consistency of the read side).

Optimistic concurrency (`row_version`) ensures two simultaneous edits do not corrupt data — the second save fails with a `409 Conflict` and the organizer is prompted to retry.

## 7. Security & Privacy

**Authorization:** Only the Owner role may edit an event (F-1.5). Authorization is enforced in the Application handler, not just the API layer (Constitution II.7).

**Input validation:** All user input is validated by FluentValidation before reaching the handler (ValidationBehavior in the MediatR pipeline). Malformed JSON or missing required fields return `400`; domain/validation rejection returns `422` (api-guidelines.md).

**No sensitive data:** Event details (title, description, schedule, location) are not sensitive personal data. No special privacy handling required.

**Session auth:** The edit endpoint requires a valid session cookie (same as F-2.1). Guest/anonymous users cannot edit events.

## 8. Edge Cases

**EC-01:** Two organizers try to edit the same event simultaneously. The first save succeeds; the second receives a `409 Conflict` due to optimistic concurrency (`row_version` mismatch). The organizer retries with the latest state.

**EC-02:** An organizer edits a Published event's schedule to a date in the past. The system accepts this (the event is already published and may have sold tickets; the organizer may be correcting a timezone issue). If past-date events need special handling, that is a future concern.

**EC-03:** An organizer reduces a ticket type's capacity from 100 to 50, but 70 tickets are already sold. The edit is rejected per INV-12 with a clear message showing the minimum allowed capacity (70 in this case).

**EC-04:** An organizer changes the event title to an empty string. Validation rejects the edit with a "title is required" message.

**EC-05:** An organizer changes the end date to be before the start date. Validation rejects the edit with an "end date must be after start date" message.

**EC-06:** An organizer with the Staff role (not Owner) attempts to edit. The request is rejected with "insufficient permissions" before the aggregate is even loaded.

**EC-07:** An organizer edits a Closed event. The request is rejected — closed events are in a terminal state and cannot be modified.

## 9. Dependencies & Risks

**Dependencies:**
- **F-2.1 (Create a draft event):** The event must exist before it can be edited. This feature is a direct follow-on.
- **F-1.5 (Define roles and permissions):** The Owner role must exist for authorization checks.
- **F-1.2 (Sign in and sign out):** The organizer must be authenticated.

**Risks:**
- **RSK-1 (Scope creep):** The boundary between "descriptive field edit" and "ticket type edit" must be kept clean. This feature covers event-level details only; ticket type changes (name, price, capacity) are handled by F-3.1/F-3.5, not here.
- **RSK-5 (Oversell):** The capacity guard (INV-12) is the critical safety mechanism. If the guard is missing or bypassed, the no-oversell guarantee breaks. This must be tested with concurrent scenarios.

## 10. Assumptions

- The event edit form shares the same field definitions as the create form (F-2.1), with the addition of pre-populated current values.
- Ticket type capacity editing is part of the ticket type management flow (F-3.1/F-3.5), not this feature. However, if capacity fields appear on the event edit form, the INV-12 guard applies.
- The organizer's dashboard (F-9.4) provides navigation to the edit form; until F-9.4 exists, the organizer accesses editing through a direct URL or the API.

## 11. Out of Scope

- **F-2.2** — Cover image upload (separate feature, already specced).
- **F-2.4** — Publishing an event (separate feature).
- **F-2.5** — Closing or cancelling an event (separate feature).
- **F-2.6** — Duplicating an event (Next phase).
- **F-3.1 / F-3.5** — Ticket type creation and editing (separate features under EP-3).
- **Slug changes** — Slug is generated at publish time (INV-15) and is not editable via this feature.
- **Audit log** — Tracking who edited what and when is F-1.9 (Later phase).
- **Real-time push of edits** — Attendees are not notified of detail changes via SignalR in MVP.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should editing a Published event's schedule trigger a notification to existing ticket holders? | ✅ No — not for MVP |
| 2 | Should the edit form show ticket type capacity fields with current sold/reserved counts to help the organizer understand guard limits? | ✅ Yes — show sold/reserved counts alongside capacity fields on the edit form |
