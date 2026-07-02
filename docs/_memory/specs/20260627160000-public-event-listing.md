---
artifact_type: spec
artifact_version: 1
id: spec-20260627160000-public-event-listing
title: Public event listing
slug: public-event-listing
filename_template: 20260627160000-public-event-listing.md
created_at: "2026-06-27T16:00:00Z"
updated_at: "2026-06-27T16:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, event-discovery]
feature_refs: ["F-4.4"]
ddd_refs: ["BC-2", "AGG-Event", "VO-EventStatus", "VO-Slug", "VO-EventSchedule"]
prd_refs: ["G-4", "QG-1", "QG-4"]
tech_refs: ["Tech §5", "Tech §7"]
db_refs: ["Tech §6"]
github_issue: 55
search_index:
  keywords: [public, event, listing, browse, discover, directory, upcoming, published, landing, index]
  bounded_contexts: ["Event Management"]
  user_personas: ["PER-A1"]
---

> GitHub: #55 (https://github.com/tranvuongduy2003/eventhub/issues/55)

# Feature: Public event listing

> Features: F-4.4  |  Status: DRAFT  |  Date: 2026-06-27
> PRD: G-4 (smooth mobile purchase), QG-1 (simplicity), QG-4 (mobile-friendly)  |  DDD: BC-2 AGG-Event, VO-EventStatus, VO-Slug  |  Tech: §5, §7

## 1. Problem & Solution

**Problem:** An attendee can only reach an event if someone shares the direct link (F-4.1). There is no way to browse what events exist on EventHub. An attendee who arrives at the platform's homepage or hears about EventHub generally has no starting point — they must already know the specific event URL. This limits organic discovery and makes the platform feel empty.

**Solution:** A public listing page at a stable URL (e.g., `/events`) that shows all currently published, upcoming events. Each event appears as a card with enough information (title, date, location, cover image, starting price) for the attendee to decide whether to click through to the full event page. The listing is accessible to anyone — no account required — and works well on mobile. Only events that are both `Published` and have a start date in the future appear; past events, drafts, closed, and cancelled events are excluded.

**Personas:**
- **PER-A1** (General attendee) — discovers events by browsing the platform rather than relying on a shared link.

**Scope:**
- **In scope:** F-4.4 — a simple browsable listing of published, upcoming events.
- **Out of scope:** Search, filtering by keyword/date/category/location (F-4.5), pagination or infinite scroll refinements, event recommendations, map view, category browsing, organizer-specific listing pages.

## 2. Acceptance Criteria

**AC-01:** GIVEN one or more Published events with future start dates, WHEN anyone opens the listing page (`/events`), THEN they see each event displayed with its title, start date/time, location summary, cover image (if set), and the lowest available ticket price.

**AC-02:** GIVEN a Published event with multiple ticket types at different prices, WHEN the event appears in the listing, THEN the listing shows the lowest ticket price (e.g., "From 150,000₫") so the attendee sees the entry point.

**AC-03:** GIVEN a Published event in the listing, WHEN the attendee clicks the event card, THEN they are taken to the full event page (`/events/{slug}`) as defined in F-4.1.

**AC-04:** GIVEN a Draft, Closed, or Cancelled event, WHEN anyone opens the listing page, THEN that event does not appear in the listing.

**AC-05:** GIVEN a Published event whose start date is in the past, WHEN anyone opens the listing page, THEN that event does not appear in the listing (only upcoming events are shown).

**AC-06:** GIVEN no Published upcoming events exist, WHEN anyone opens the listing page, THEN they see an empty state message (e.g., "No upcoming events") rather than a blank page.

**AC-07:** GIVEN the listing page is opened on a mobile phone, WHEN the attendee views it, THEN the event cards are readable and tappable without horizontal scrolling or zooming (mobile-friendly — QG-4).

**AC-08:** GIVEN the listing page is opened by a visitor with no account, WHEN the page loads, THEN it loads fully — no sign-in is required.

**AC-09:** GIVEN multiple Published upcoming events, WHEN the listing page loads, THEN the events are sorted by start date in ascending order (soonest first), giving the attendee a natural chronological browsing experience.

## 3. Domain & Business Rules

Referenced from `domain-model-specification.md`:

- **VO-EventStatus:** Only `Published` events appear in the listing. Draft, Closed, and Cancelled events are excluded. This aligns with the publish lifecycle — an event becomes publicly discoverable only after the organizer explicitly publishes it (F-2.4).
- **VO-EventSchedule:** The start date is used to filter out past events (AC-05) and to sort the listing (AC-09). Only events with a future start date are shown.
- **VO-Slug:** Each listing card links to the event's stable public page at `/events/{slug}`.
- **AGG-Event attributes used:** title, start date, location summary, cover image reference, slug, status. No new attributes or invariants are introduced.
- **Ticket types and pricing (ENT-TicketType):** The listing shows the lowest available ticket price across the event's ticket types. This is a read-only presentation concern — it does not affect inventory, reservations, or pricing invariants.
- **No-oversell (INV-10):** A sold-out event still appears in the listing (it is still published and upcoming). The listing card can indicate sold-out status, but the event remains discoverable. The attendee sees the sold-out state on the full event page.
- **Transparent pricing (F-3.3):** The price shown in the listing is the final all-inclusive price — the same price the attendee would see on the event page and at checkout.

## 4. UI Behavior & API Contract

### 4.1 Listing page

| Aspect | Behavior |
|--------|----------|
| **URL** | `/events` (public, no auth required) |
| **Content** | Cards for each published, upcoming event |
| **Sort order** | Start date ascending (soonest first) |
| **Empty state** | Friendly message when no events match |
| **Mobile** | Responsive card layout; single column on small screens, grid on larger screens |

### 4.2 Event card

Each card in the listing displays:

| Field | Source | Notes |
|-------|--------|-------|
| Cover image | `VO-CoverImageRef` | Shown as thumbnail; fallback placeholder if not set |
| Title | Event title | Truncated with ellipsis if too long for the card |
| Start date/time | `VO-EventSchedule` start | Formatted in the event's time zone; human-readable (e.g., "Sat, Jun 28, 7:00 PM") |
| Location summary | `VO-EventLocation` | City or "Online"; not the full address |
| Starting price | Lowest ticket type price | "From {price}" or "Free" for zero-price events; "Sold out" if no availability |

### 4.3 API endpoint

A public read endpoint that returns the listing data:

| Aspect | Detail |
|--------|--------|
| **Method / path** | `GET /api/events` (or `GET /api/events/public`) |
| **Auth** | None required (public) |
| **Query params** | None for the initial implementation (F-4.5 adds search/filter later) |
| **Response** | Array of event listing items, each containing: slug, title, start date, location summary, cover image URL, lowest ticket price, sold-out flag |
| **Status codes** | `200` with array (empty array if no events match) |

### 4.4 Frontend routing

| Route | Component | Notes |
|-------|-----------|-------|
| `/events` | Event listing page | Public; no auth guard |
| `/events/:slug` | Event detail page | Existing F-4.1 page |

## 5. Data & Storage Impact

- **PostgreSQL:** No schema changes. The listing query reads from the existing `Event` and `TicketType` tables. A query filters on `Status = Published` and `StartDate > now()`, ordered by `StartDate asc`. The lowest ticket price is derived from the `TicketType` entities associated with each event.
- **Redis:** The listing response is a good candidate for caching — the data changes infrequently (only when an event is published, edited, or tickets sell out). A 30–60 second TTL is appropriate. Cache invalidation can be driven by the same events that affect the public event page cache.
- **MinIO:** No impact — cover image URLs are already stored; the listing reuses them.
- **RabbitMQ:** No impact — this is a read-only concern.

## 6. Real-Time & Consistency

- **Cache freshness:** The listing is eventually consistent with the write side, reflecting new publications, edits, or sold-out status within the cache TTL (30–60 seconds). This is acceptable for a discovery page — an attendee refreshing within a minute sees the latest state.
- **No SignalR:** The listing page does not require realtime push updates. An attendee browsing the listing can refresh to see new events. Realtime listing updates (e.g., live sold-out indicators) are out of scope for this feature.
- **No integration events:** The listing is a read-only query — no domain or integration events are emitted. It consumes data already produced by EP-2 (event lifecycle) and EP-3 (ticket types).

## 7. Security & Privacy

- **Public data only:** The listing shows only publicly visible event metadata (title, date, location, cover image, ticket price). No personal data, attendee information, or unpublished event details are exposed.
- **Draft event protection:** Only `Published` events appear. Draft events are invisible in the listing, consistent with F-4.1's protection of non-published events.
- **No authentication required:** The listing page and its API endpoint are fully public (AC-08). No session or token is needed.
- **No new attack surface:** The listing endpoint is a read-only public query with no user input parameters in this iteration. F-4.5 will add query parameters for search/filter, which will require input validation.

## 8. Edge Cases

**EC-01:** An event is Published but all its ticket types are sold out. The event still appears in the listing (it is still upcoming and published). The card shows "Sold out" where the price would normally appear. The attendee can click through to see the full event page.

**EC-02:** An event is Published but has no ticket types (should not happen per INV-11 — publish requires at least one ticket type). If it somehow exists, the listing shows the event without a price or with a "Tickets not available" indicator. This is a defensive case.

**EC-03:** An event's start time zone differs from the attendee's local time zone. The listing shows the date/time in the event's configured time zone (the same as F-4.1), not the attendee's local time. Time zone conversion is a potential future enhancement.

**EC-04:** A very large number of published upcoming events exist (hundreds+). The initial implementation returns all matching events without pagination. If performance becomes a concern, pagination or infinite scroll can be added. At the project's intended scale (small events, modest demand — ASM-2), this is unlikely to be an issue.

**EC-05:** An event's title or location is very long. The listing card truncates with ellipsis to maintain a clean layout. The full text is visible on the event page (F-4.1).

**EC-06:** An event has no cover image. The listing card shows a placeholder image or a styled fallback (e.g., a colored gradient with the event title initials). The card remains tappable and links to the event page.

**EC-07:** An event is Closed (no new purchases) but the start date is still in the future. Per the business rules, Closed events do not appear in the listing (AC-04). The event is no longer accepting purchases, so surfacing it in a discovery page would be misleading.

**EC-08:** The listing is opened at the exact moment an event transitions from future to past (start time passes). The cache TTL (30–60 seconds) means the event may appear briefly after its start time. This is acceptable — the event page itself handles the state correctly.

## 9. Dependencies & Risks

**Dependencies:**
- F-4.1 (Shareable public event page) — the full event page that each listing card links to must exist.
- F-2.4 (Publish an event) — events must be publishable with a status and slug.
- F-3.1 (Define a ticket type) — ticket types with prices must exist to show the starting price.

**Risks:**
- **R-01 (Low):** Query performance — joining events with ticket types to compute the lowest price could be slow with many events. Mitigation: at the intended scale (small events), this is acceptable; index on `Status` + `StartDate` for efficient filtering.
- **R-02 (Low):** Stale cache — an event sells out or is cancelled, but the listing still shows it. Mitigation: 30–60 second cache TTL keeps staleness brief; event page (F-4.1) always shows current state.
- **R-03 (Low):** Empty listing for new platforms — a brand-new EventHub instance has no published events, making the listing page feel empty. Mitigation: the empty state message (AC-06) handles this gracefully; the primary discovery path remains the shared link (F-4.1).

## 10. Assumptions

- The listing page URL is `/events` — a natural, intuitive path for event browsing.
- Events are sorted by start date ascending (soonest first) as the default. No sort options are provided in this iteration.
- The listing shows all published upcoming events regardless of location — no geo-filtering or location-based sorting.
- The listing is a simple, flat page — no categories, tags, or faceted filtering. F-4.5 adds search/filter later.
- The listing page reuses the same public API pattern as the event detail page — no authentication, no user-specific data.
- The "lowest price" shown is the minimum across all non-sold-out ticket types. If all ticket types are sold out, the card shows "Sold out."
- Pagination is not required for the initial implementation at the project's intended scale.

## 11. Out of Scope

- **Search and filter (F-4.5):** Keyword search, date range filtering, category/location filtering — these are the next feature in the discovery epic.
- **Pagination or infinite scroll:** Not needed at the intended scale; can be added if the event count grows.
- **Event recommendations or personalization:** No "events you might like" or personalized ordering.
- **Map view:** No geographic visualization of events.
- **Category or tag browsing:** No category taxonomy or tag-based navigation.
- **Organizer-specific listing pages:** No per-organizer public event directory.
- **Past event archive:** Past events are not shown; no history or archive view.
- **Social features:** No "share this listing" or social proof indicators (e.g., "X friends are attending").
- **Analytics:** No tracking of listing views, click-through rates, or conversion metrics.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the listing page URL be `/events` or `/` (homepage as the listing)? **Resolved:** `/events`. | ✅ |
| 2 | Should sold-out events appear in the listing with a "Sold out" badge, or should they be hidden? **Resolved:** Show them with a "Sold out" indicator. | ✅ |
| 3 | Should the listing include a simple count of remaining tickets per type, or just the "Sold out" indicator? **Resolved:** Just the price and sold-out indicator — detailed availability is for the event page. | ✅ |
