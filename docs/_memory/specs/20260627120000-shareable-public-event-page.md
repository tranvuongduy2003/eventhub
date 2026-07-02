---
artifact_type: spec
artifact_version: 1
id: spec-20260627120000-shareable-public-event-page
title: Shareable public event page
slug: shareable-public-event-page
filename_template: 20260627120000-shareable-public-event-page.md
created_at: "2026-06-27T12:00:00Z"
updated_at: "2026-06-27T12:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, event-discovery]
feature_refs: ["F-4.1"]
ddd_refs: ["BC-2", "AGG-Event", "ENT-TicketType", "VO-Slug", "VO-EventStatus", "INV-14"]
prd_refs: ["DEC-3", "QG-1", "QG-2", "QG-4", "QG-5"]
tech_refs: ["Tech §5", "Tech §7"]
db_refs: ["Tech §6"]
github_issue: 49
search_index:
  keywords: [public, event, page, shareable, link, slug, ticket, price, discovery, attendee, buy]
  bounded_contexts: ["Event Management"]
  user_personas: ["PER-A1", "PER-A2"]
---

> GitHub: #49 (https://github.com/tranvuongduy2003/eventhub/issues/49)

# Feature: Shareable public event page

> Features: F-4.1  |  Status: DRAFT  |  Date: 2026-06-27
> PRD: DEC-3 (MVP scope), QG-1 (simplicity), QG-2 (transparent pricing), QG-4 (mobile-friendly), QG-5 (correctness)  |  DDD: BC-2 AGG-Event, INV-14  |  Tech: §5, §7

## 1. Problem & Solution

**Problem:** An organizer has published an event (F-2.4) with ticket types and transparent prices (F-3.3), but there is no way for potential attendees to actually see the event. The event exists in the system but is invisible to the people who would buy tickets.

**Solution:** Every published event gets a public page at a stable, shareable URL (`/events/{slug}`). Anyone — with or without an account — can open this link and see the event's title, description, schedule, location, cover image, and every ticket type with its final all-inclusive price. The page provides a clear entry point to begin purchasing (EP-5). Events in non-purchasable states (Draft, Closed, Cancelled) display their status instead of a buy option.

**Personas:** PER-A1 (general attendee) and PER-A2 (group buyer) — the people who discover events and buy tickets.

**Scope:** F-4.1 only. Mobile-friendliness (F-4.2), rich link previews (F-4.3), public listing (F-4.4), and search (F-4.5) are separate features.

## 2. Acceptance Criteria

**AC-01:** GIVEN a Published event with ticket types, WHEN anyone opens the event's stable URL (`/events/{slug}`), THEN they see the event title, description, date/time (with time zone), location, cover image (if set), and each ticket type with its name and final all-inclusive price.

**AC-02:** GIVEN a Published event, WHEN a visitor with no account opens the event's URL, THEN the page loads fully and displays all event details — no sign-in is required.

**AC-03:** GIVEN a Published event with ticket types, WHEN the visitor views the page, THEN each ticket type shows a "buy" or equivalent call-to-action that leads into the purchase flow (EP-5).

**AC-04:** GIVEN a Draft event, WHEN anyone attempts to open its URL, THEN they see a message indicating the event is not yet available (no event details or ticket information are shown).

**AC-05:** GIVEN a Closed event, WHEN anyone opens its URL, THEN they see the event details (title, description, schedule, location, cover) but the buy option is replaced with a message indicating sales are closed.

**AC-06:** GIVEN a Cancelled event, WHEN anyone opens its URL, THEN they see the event details with a clear message that the event has been cancelled and no buy option is available.

**AC-07:** GIVEN a Published event, WHEN the visitor views the page, THEN the price shown for each ticket type is the final all-inclusive price — the same amount that will be charged at checkout (F-3.3, DEC-1).

**AC-08:** GIVEN a Published event with a cover image, WHEN the visitor views the page, THEN the cover image is displayed. GIVEN no cover image, THEN the page still loads and displays all other content without error.

**AC-09:** GIVEN a Published event with multiple ticket types, WHEN the visitor views the page, THEN all ticket types are listed, each with its own name and price.

**AC-10:** GIVEN a non-existent slug, WHEN anyone attempts to open that URL, THEN they see a clear "event not found" message.

## 3. Domain & Business Rules

Referenced from `domain-model-specification.md`:

- **VO-Slug:** The stable, URL-safe, unique identifier generated when an event is published (F-2.4). The public page URL is `/events/{slug}`. The slug is permanent — it does not change after publishing.
- **VO-EventStatus:** Determines what the page displays:
  - `Published` — full details + ticket types with prices + buy CTA
  - `Draft` — "not available" message (event is not public)
  - `Closed` — event details visible, buy option disabled, "sales closed" message
  - `Cancelled` — event details visible, "event cancelled" message
- **INV-14 (reservation guard):** A ticket type is only purchasable when the event is `Published` and within its `SalesWindow` (if set). The public page reflects this — a ticket type outside its sales window shows as unavailable but still displays its price.
- **Transparent pricing (F-3.3):** The price displayed on the public page is the final price. No fees, surcharges, or taxes are added later in the purchase flow. This is a core product promise (`product-requirements.md` QG-2, DEC-1).
- **Cover image reference:** The cover image is stored as an object reference (`VO-CoverImageRef`) in MinIO. The public page fetches the image from object storage using this reference.
- **Guest access:** The public page is a read-only surface that requires no authentication. This aligns with the attendee lifecycle (`product-requirements.md` §5.2): find the event → see the price → buy.

## 4. API Contract

**Endpoint:** `GET /api/events/{slug}`

**Auth:** None required — this is a public endpoint.

**Success (200):** Returns the event's public representation:

| Field | Type | Notes |
|-------|------|-------|
| `title` | string | Event title |
| `description` | string | Full description |
| `startDateTime` | ISO-8601 | With time zone |
| `endDateTime` | ISO-8601 | With time zone |
| `location` | object | Physical address or `Online` marker |
| `coverImageUrl` | string (nullable) | URL to the cover image in object storage; null if not set |
| `status` | string | `Published`, `Closed`, or `Cancelled` |
| `ticketTypes` | array | Each entry: `id`, `name`, `finalPrice` (amount + currency), `status` (`available`, `soldOut`, `notYetOnSale`) |
| `purchasable` | boolean | `true` only if event is `Published` and at least one ticket type is available for purchase |

**Error responses:**

| Status | Code | Condition |
|--------|------|-----------|
| 404 | `EVENT_NOT_FOUND` | No published/closed/cancelled event exists for this slug |
| 404 | `EVENT_NOT_FOUND` | Event exists but is in `Draft` status (not public) — same response as not found to avoid leaking draft events |

**Note:** Draft events return 404 (not a distinct "not available" response) to prevent information leakage about unpublished events. The UI maps 404 to an "event not found" page.

## 5. Data & Storage Impact

- **PostgreSQL:** Read-only query against the `events` table, joining `ticket_types` for the event. Uses the `slug` index for lookup. `AsNoTracking` since this is a read query. No writes.
- **Redis:** The public event page is a candidate for response caching (TTL of 30–60 seconds) since event data changes infrequently. Cache invalidation on event update/publish/close/cancel can be addressed in a follow-up; for MVP, a short TTL is sufficient.
- **MinIO:** The cover image URL is resolved from the stored object reference. The API returns the URL; the browser fetches the image directly from MinIO.
- **RabbitMQ:** No impact — this is a pure read.

## 6. Real-Time & Consistency

- **Read consistency:** The public page reads from PostgreSQL (authoritative) or a short-lived Redis cache. Eventual consistency is acceptable — if the organizer edits the event, the page reflects the change within the cache TTL.
- **Ticket availability:** The `ticketTypes` array shows real-time availability status (`available`, `soldOut`, `notYetOnSale`). For the MVP, this is computed on each request (or served from a short cache). Exact real-time accuracy is not required on the public page — the purchase flow (EP-5) performs the authoritative availability check.
- **N/A for SignalR:** Real-time push to the public page is not needed for MVP. Live inventory updates (F-11.1) are a later feature.

## 7. Security & Privacy

- **No authentication required:** This is a public, read-only surface. No session or guest identity is needed to view the page.
- **Draft event protection:** Draft events are not exposed through the public endpoint. The API returns 404 for any event that is not Published, Closed, or Cancelled. This prevents organizers' work-in-progress from being discoverable.
- **No sensitive data:** The response contains only event metadata, ticket types, and prices. No personal data, order information, or payment details are exposed.
- **Rate limiting (future consideration):** The public endpoint is unauthenticated and could be subject to abuse. For MVP with small event volumes, this is low risk. Rate limiting can be added if needed.
- **Image serving:** Cover images are served from MinIO. The URL is publicly accessible; no signed URLs are needed for event cover images in the MVP.

## 8. Edge Cases

**EC-01:** An event is Published, then all ticket types are removed or deleted. The public page still loads and shows event details, but the `ticketTypes` array is empty and `purchasable` is `false`. The buy CTA is disabled or replaced with a "no tickets available" message.

**EC-02:** A ticket type has a scheduled sales window (F-3.8) that has not started yet. The ticket type appears on the page with its price but shows as "not yet on sale" and is not included in the `purchasable` calculation.

**EC-03:** A ticket type is sold out (F-3.4). The ticket type appears on the page with its price but is marked as "sold out" and is not purchasable.

**EC-04:** The cover image URL in MinIO becomes unavailable (e.g., object deleted outside the application). The page renders without the cover image (nullable field) — no broken image or error.

**EC-05:** An attendee shares the URL after the event is Closed or Cancelled. The page still loads and shows event details with the appropriate status message — the URL does not break.

**EC-06:** The slug in the URL does not match any event (typo, old link, random string). The API returns 404; the frontend shows an "event not found" page.

**EC-07:** A visitor tries to access the event page using the internal event ID instead of the slug. The API does not expose events by ID on the public endpoint — only by slug. The request returns 404.

**EC-08:** The event description contains long text or special characters. The page renders the full description without truncation on the server side. Truncation or "read more" is a UI-level concern (F-4.2 / design).

## 9. Dependencies & Risks

**Dependencies:**
- F-2.4 (Publish an event) — the event must be published and have a slug before the public page exists
- F-3.3 (Transparent pricing) — the final all-inclusive price must be available to display
- F-3.1 (Define a ticket type) — published events have at least one ticket type (per F-2.4's publish gate)

**Risks:**
- **Low:** The public endpoint is read-only and simple. The main risk is caching correctness — showing stale data after an organizer edits the event. A short TTL (30–60s) mitigates this for MVP.
- **Low:** Cover image availability depends on MinIO. If MinIO is down, the page degrades gracefully (no image, but all other content loads).

## 10. Assumptions

- The public page URL format is `/events/{slug}` — the slug is the permanent, stable identifier (set in F-2.4).
- Draft events are hidden from the public endpoint entirely (404), not shown as "coming soon." This is the simpler, safer approach for MVP.
- The API returns the full event description; truncation or rich rendering is a frontend/design concern.
- The public page does not show attendee counts, sales progress, or "X tickets remaining" — that is a future feature (F-11.3) and an organizer concern.
- The currency is the single configured currency for the platform (no multi-currency — `product-requirements.md` §6.2).
- The public endpoint does not require rate limiting for MVP given the small event scale (`product-requirements.md` ASM-2).

## 11. Out of Scope

- **Mobile-friendliness (F-4.2):** Responsive layout, touch targets, and phone-optimized UX are a separate feature.
- **Rich link previews (F-4.3):** Open Graph / Twitter Card meta tags for social sharing.
- **Public event listing (F-4.4):** A browseable page of public events.
- **Search and filter (F-4.5):** Finding events by keyword, date, or category.
- **SEO optimization:** Server-side rendering, meta descriptions, structured data — not required for MVP.
- **Event sharing tools:** "Share" buttons, copy-link UI — the organizer shares the URL manually for MVP.
- **Analytics / tracking:** Page views, referral sources — not in scope.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should Draft events return a distinct "coming soon" page instead of 404, to allow organizers to preview their link before publishing? **Resolved:** No — 404 for Draft events to prevent information leakage. Organizers can preview via the authenticated edit view. | ✅ |
| 2 | Should the public page response include a `ticketTypes` entry with `remainingCount` (tickets left), or only `soldOut` / `available` status? **Resolved:** Only status (`available`, `soldOut`, `notYetOnSale`) — exact remaining counts are not exposed on the public page for MVP. | ✅ |
| 3 | Should the cover image be served through the API (proxy) or directly from MinIO? **Resolved:** The API returns the MinIO URL and the browser fetches directly — no proxy needed for public assets. | ✅ |
