---
artifact_type: spec
artifact_version: 1
id: spec-20260619100000-create-draft-event
title: Create a draft event
slug: create-draft-event
filename_template: 20260619100000-create-draft-event.md
created_at: 2026-06-19T10:00:00+07:00
updated_at: 2026-06-20T10:00:00+07:00
status: draft
owner: product
tags: [spec, eventhub, event-creation-management]
feature_refs: [F-2.1]
ddd_refs: [BC-2, AGG-Event, INV-11, INV-15, EVT-EventPublished]
prd_refs: [DEC-3, QG-1, QG-4, QG-7, PER-O1, PER-O2]
tech_refs: [Tech §5, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 11
search_index:
  keywords:
    - create event
    - draft event
    - event creation
    - event management
    - event title
    - event schedule
    - event location
    - online event
    - organizer
    - event lifecycle
  bounded_contexts: [BC-2 Event Management]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: [#11](https://github.com/tranvuongduy2003/eventhub/issues/11)

# Feature: Create a draft event

> Features: F-2.1  |  Status: DRAFT  |  Date: 2026-06-19
> PRD: DEC-3 (MVP spine), QG-1 (simplicity)  |  DDD: BC-2 · AGG-Event · INV-11  |  Tech: §5–7 (PostgreSQL, session auth)

## 1. Problem & Solution

**Problem:** An organizer cannot begin setting up an event until they have a way to create one. Without event creation, there is nothing to attach ticket types to (F-3.1), nothing to publish (F-2.4), and nothing for attendees to discover (EP-4). Event creation is the entry point of the organizer's lifecycle and the first step in the MVP spine after authentication.

**Solution:** A signed-in organizer opens an event creation form, provides the core details — title, start and end date-time with a time zone, and a location (physical address or online) — and submits. A new event is created in **Draft** status, owned by the organizer. The draft is private: only the owner can see and edit it. The organizer can return later to add ticket types, upload a cover image, and eventually publish.

**Personas:** PER-O1 (individual organizer — wants fast, simple setup), PER-O2 (small group / club organizer — wants a structured starting point for their event).

**Scope:**
- **In:** F-2.1 only — create a draft event with core details, validate required fields, persist with Draft status and ownership.
- **Out:** F-2.2 cover image upload; F-2.3 edit event details; F-2.4 publish (requires ticket types per F-3.1); F-2.5 close/cancel; F-2.6 duplicate; F-2.7 multiple occurrences; ticket type creation (F-3.1); slug generation for published events (INV-15 applies at publish time, not at draft creation).

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in as an organizer and I provide a valid title (non-empty, within length limit), a start date-time, an end date-time that is at or after the start, a time zone, and a location type (physical address or online), WHEN I submit the event creation form, THEN a new event is created in **Draft** status, owned by me with the Owner role (F-1.5), and I receive a success response containing the event identifier, status, and created timestamp.

**AC-02:** GIVEN I am signed in and I provide a physical address, WHEN I submit, THEN the event is created with the physical address stored as the location.

**AC-03:** GIVEN I am signed in and I mark the event as online (no physical address required), WHEN I submit, THEN the event is created with the location recorded as online.

**AC-04:** GIVEN I am signed in and I leave the title empty or provide only whitespace, WHEN I submit, THEN no event is created and I receive a clear validation message on the title field.

**AC-05:** GIVEN I am signed in and I provide an end date-time that is before the start date-time, WHEN I submit, THEN no event is created and I receive a clear validation message that end must be at or after start.

**AC-06:** GIVEN I am signed in and I provide an invalid or missing time zone, WHEN I submit, THEN no event is created and I receive a clear validation message on the time zone field.

**AC-07:** GIVEN I am signed in and I select a physical location but leave the address fields empty, WHEN I submit, THEN no event is created and I receive a clear validation message on the location fields.

**AC-08:** GIVEN I am signed in and I select online but also provide a physical address, WHEN I submit, THEN the event is created as online (the physical address is ignored; online takes precedence) — or the form prevents this combination client-side.

**AC-09:** GIVEN a draft event has been created, WHEN I view my event list or the event detail, THEN the draft is visible only to me — other organizers and anonymous users cannot see it.

**AC-10:** GIVEN I am not signed in, WHEN I attempt to access the event creation endpoint or page, THEN I am redirected to the sign-in page (or receive a 401).

**AC-11:** GIVEN a transient server failure during event creation, WHEN the request fails, THEN I see a generic retry message, no partial event is left in an inconsistent state, and I can try again.

## 3. Domain & Business Rules

Align with BC-2 Event Management and AGG-Event:

| Rule | Detail |
|------|--------|
| **Ownership** | Every event is owned by exactly one organizer, identified by `OrganizerId` (the signed-in user's `UserId`). Ownership is set at creation and cannot be changed. On creation, the organizer is assigned the **Owner role** (F-1.5) via `EventUserRole`, handled by the `EventCreatedEvent` domain event handler. |
| **Status** | A new event always starts as **Draft**. The Draft → Published transition (F-2.4) requires at least one ticket type (INV-11); that check is out of scope here. |
| **Title** | Required. Trimmed; 1–200 characters after trim; must not be only whitespace. Not required to be unique — multiple events may share a title. |
| **Schedule (VO-EventSchedule)** | Required. Start date-time + end date-time + IANA time zone identifier. End must be ≥ start. Both stored as UTC internally; the time zone is preserved for display. |
| **Location (VO-EventLocation)** | Required. Exactly one of: (a) a physical address with at least venue name and city, or (b) online (a flag). The organizer picks one mode; the form adapts. |
| **Description** | Optional free-text field for the event description. No length limit enforced at creation (rich editing deferred). |
| **Slug** | Not generated at draft creation. Slugs are assigned when an event is published (INV-15: unique among published events). A draft has no public URL. |
| **Cover image** | Not part of this slice (F-2.2). |
| **Visibility** | A Draft event is visible only to its owner. Queries from other users or anonymous visitors must not return it. |
| **Behavior** | `CreateDraft` on the aggregate: validate invariants → set ownership and status → raise domain event → persist. |
| **Event** | On successful creation, emit `EVT-DraftCreated` (domain-scope event; no integration fan-out needed for MVP). |

## 4. UI Behavior

### Event creation page (authenticated, organizer)

- Route accessible from the organizer dashboard / home area (e.g. "Create event" button).
- Mobile-first layout (QG-4): single-column form, full-width primary button, labels on all fields (QG-7).
- Fields:
  1. **Event title** — text input, required.
  2. **Start date** — date picker, required.
  3. **Start time** — time picker, required.
  4. **End date** — date picker, required (defaults to same as start date).
  5. **End time** — time picker, required (defaults to 1 hour after start time).
  6. **Time zone** — dropdown or auto-detected from browser, required; shows IANA zone names (e.g. "Asia/Ho_Chi_Minh").
  7. **Location type** — toggle or radio: "Physical address" / "Online".
  8. **Physical address** (shown when type = physical): single text input for the full address, required.
  9. **Description** — textarea, optional.
- Primary action: **Create event** (disabled while submitting; show loading state).
- Secondary action: **Cancel** → return to organizer dashboard.
- Client-side validation mirrors server rules (Zod-equivalent) for immediate feedback on blur/change.
- Server validation errors map to the relevant field; unexpected failures show a single root-level message.
- On success: navigate to the new event's detail/edit page (organizer view) with the draft state visible.
- On error: clear sensitive fields if any; show errors inline.

### API contract (product level)

| Operation | Method & path | Success | Failure |
|-----------|---------------|---------|---------|
| Create draft event | `POST /api/events` | `201 Created` — body includes event id, title, schedule, location, status (Draft), organizer id, created timestamp | `400` malformed JSON; `401` no session; `422` validation or business rule failure with RFC 7807 problem details including stable `code` and field errors |

The endpoint requires a valid session cookie. The response does not include ticket types, cover image, or slug (those are added in later features).

## 5. Data & Storage Impact

| Store | Impact |
|-------|--------|
| **PostgreSQL** | New row in the event table: stable event id, organizer id (FK to user), title, start time (timestamptz), end time (timestamptz), time zone (text), physical address (nullable when online), is online (bool), status (enum: Draft), created timestamp, updated timestamp, row version (for optimistic concurrency). No slug yet (set at publish). |
| **Redis** | None for this slice. |
| **MinIO** | None for this slice (cover image is F-2.2). |
| **RabbitMQ** | None for this slice. `EVT-DraftCreated` is a domain-scope event handled in-process. |

## 6. Real-Time & Consistency

N/A — event creation is a synchronous request/response flow. No SignalR push or integration-event fan-out is required. The draft is immediately visible to the owner after creation (strong consistency within the same database).

## 7. Security & Privacy

- **Session required:** The endpoint rejects unauthenticated requests with 401 (same cookie session mechanism as EP-1).
- **Ownership:** The `OrganizerId` is taken from the session (`ICurrentUserAccessor`), not from the request body, so a user cannot create an event on someone else's behalf.
- **Authorization:** Only the owner can view or act on a draft. Query filters enforce this at the data-access level.
- **Input validation:** Title, schedule, and location are validated server-side; no raw user input is stored without sanitization.
- **Privacy (QG-6):** No personal attendee data is collected at event creation. The organizer's identity is already established (EP-1).

## 8. Edge Cases

**EC-01:** Organizer submits the creation form twice quickly (double-click) — the system should either deduplicate (idempotency key) or create two distinct drafts. For MVP, creating two drafts is acceptable; the organizer can delete the duplicate later.

**EC-02:** Start and end are the same instant — allowed (a zero-duration event is unusual but not invalid; the organizer can edit later).

**EC-03:** Time zone is valid but unusual (e.g. a deprecated IANA zone) — accept it; store the IANA identifier as-is for display.

**EC-04:** Title contains leading/trailing spaces — trimmed before save; inner spaces and Unicode allowed.

**EC-05:** Description is very long — accepted for MVP; rich-text editing and length limits are deferred.

**EC-06:** Organizer's session expires between loading the form and submitting — the submit fails with 401; the form data is lost. The organizer must sign in again and re-enter (acceptable for MVP).

**EC-07:** Physical address fields contain special characters or Unicode — accepted; stored as-is.

**EC-08:** Online location selected but address fields partially filled — the address fields are ignored; only the "online" flag is persisted.

**EC-09:** End date-time is far in the future (e.g. years ahead) — accepted; no upper bound enforced for MVP.

## 9. Dependencies & Risks

| Type | Item |
|------|------|
| **Upstream** | F-1.2 (sign-in) — the organizer must have an active session. |
| **Downstream** | F-2.2 (cover image), F-2.3 (edit details), F-2.4 (publish — requires ticket types from F-3.1), F-2.5 (close/cancel), F-3.1 (add ticket type to the event). |
| **Risks** | Scope creep into edit flows (mitigated: edit is F-2.3); ambiguity on physical vs online location UX (mitigated: clear toggle with conditional fields); slug generation timing confusion (mitigated: slug deferred to publish per INV-15). |

## 10. Assumptions

- The organizer dashboard or home area exists as a destination after sign-in (from F-1.1/F-1.2); the "Create event" entry point is placed there.
- The event creation page is a dedicated route, not a modal overlay.
- Time zone detection uses the browser's `Intl.DateTimeFormat().resolvedOptions().timeZone` as a default; the organizer can override.
- Description is plain text for MVP; rich-text editing (markdown or WYSIWYG) is deferred.
- No draft auto-save or draft recovery on browser close for MVP.
- The API endpoint for event creation is `POST /api/events` (aligns with REST conventions and the existing API structure).
- A draft event has no public URL or slug; the slug is generated at publish time (F-2.4).

## 11. Out of Scope

- F-2.2 cover image upload and display.
- F-2.3 editing event details after creation.
- F-2.4 publishing (requires ticket types per INV-11).
- F-2.5 close or cancel an event.
- F-2.6 duplicate an event.
- F-2.7 multiple occurrences / sessions.
- F-3.1 ticket type creation (belongs to the event detail/edit flow).
- Slug generation (deferred to publish).
- Rich-text or markdown description editing.
- Draft auto-save or recovery.
- Event templates or categories.
- Recurring events.

## 12. Resolved Decisions

| # | Question | Decision | Date |
|---|----------|----------|------|
| 1 | Should the form support "Save as draft" explicitly, or is the only action "Create event" which always creates a draft? | **Single "Create event" action** — always creates a draft. Explicit save-as-draft is redundant when creation always produces a draft. | 2026-06-19 |
| 2 | What is the maximum allowed duration for an event (start to end)? | **No limit.** Organizer responsibility. | 2026-06-19 |
| 3 | Should the creation response include a redirect URL to the event detail page, or should the client construct it from the event id? | **Client constructs from event id** (consistent with REST conventions). | 2026-06-19 |
