---
artifact_type: spec
artifact_version: 1
id: spec-20260627170000-search-and-filter
title: Search and filter
slug: search-and-filter
filename_template: 20260627170000-search-and-filter.md
created_at: "2026-06-27T17:00:00Z"
updated_at: "2026-06-27T17:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, event-discovery]
feature_refs: ["F-4.5"]
ddd_refs: ["BC-2", "AGG-Event", "VO-EventStatus", "VO-Slug", "VO-EventSchedule", "VO-EventLocation"]
prd_refs: ["G-4", "QG-1", "QG-4"]
tech_refs: ["Tech §5", "Tech §6", "Tech §7"]
db_refs: ["Tech §6"]
github_issue: 57
search_index:
  keywords: [search, filter, keyword, date, category, location, discover, browse, query, listing]
  bounded_contexts: ["Event Management"]
  user_personas: ["PER-A1"]
---

> GitHub: #57 (https://github.com/tranvuongduy2003/rebuilt-eventhub/issues/57)

# Feature: Search and filter

> Features: F-4.5  |  Status: DRAFT  |  Date: 2026-06-27
> PRD: G-4 (smooth mobile purchase), QG-1 (simplicity), QG-4 (mobile-friendly)  |  DDD: BC-2 AGG-Event, VO-EventSchedule, VO-EventLocation  |  Tech: §5, §6, §7

## 1. Problem & Solution

**Problem:** The public event listing (F-4.4) shows all published, upcoming events in chronological order. As the number of events grows, an attendee must scroll through the entire list to find something specific. There is no way to narrow results by what the attendee is looking for — a keyword, a date range, or a location. This makes discovery frustrating when more than a handful of events exist.

**Solution:** Add search and filter capabilities to the existing event listing page. An attendee can type a keyword to match against event titles and descriptions, select a date range (today, this week, this month, or custom), and filter by location. Filters combine — keyword + date + location narrow together. The listing updates to show only matching events. All search and filter state is reflected in the URL so results are shareable and bookmarkable.

**Personas:**
- **PER-A1** (General attendee) — finds events by searching or filtering rather than scrolling the full list.

**Scope:**
- **In scope:** F-4.5 — keyword search, date range filtering, location filtering on the public event listing.
- **Out of scope:** Category/tag-based filtering (no category taxonomy exists in the domain model), advanced faceted search, autocomplete suggestions, search result ranking by relevance, saved searches, map-based filtering, organizer-specific filtering.

## 2. Acceptance Criteria

**AC-01:** GIVEN one or more Published upcoming events exist, WHEN an attendee types a keyword in the search field, THEN the listing narrows to events whose title or description contains the keyword (case-insensitive, partial match).

**AC-02:** GIVEN one or more Published upcoming events exist, WHEN an attendee selects a date range (e.g., "This week"), THEN the listing narrows to events whose start date falls within the selected range.

**AC-03:** GIVEN one or more Published upcoming events exist, WHEN an attendee selects a location filter, THEN the listing narrows to events whose location matches the selected value.

**AC-04:** GIVEN an attendee has applied a keyword search and a date filter, WHEN both are active, THEN the listing shows only events matching both criteria (filters are combinational, not exclusive).

**AC-05:** GIVEN an attendee has applied one or more filters, WHEN they clear or reset the filters, THEN the listing returns to showing all published upcoming events.

**AC-06:** GIVEN an attendee has applied filters, WHEN no events match the combined criteria, THEN the listing shows an empty state message (e.g., "No events found") with a suggestion to adjust filters.

**AC-07:** GIVEN an attendee has applied filters, WHEN they view the URL, THEN the URL contains the filter parameters as query strings (e.g., `/events?q=workshop&date=this-week&location=ho-chi-minh`), making the filtered view shareable and bookmarkable.

**AC-08:** GIVEN an attendee opens a URL with filter parameters in the query string, WHEN the page loads, THEN the listing is pre-filtered according to the URL parameters and the filter controls reflect the active state.

**AC-09:** GIVEN the listing page is opened on a mobile phone, WHEN the attendee uses the search and filter controls, THEN the controls are usable without horizontal scrolling or zooming (mobile-friendly — QG-4).

**AC-10:** GIVEN an attendee types a keyword, WHEN they stop typing for a brief moment (debounce), THEN the listing updates to show matching results — the listing does not update on every keystroke.

**AC-11:** GIVEN an attendee applies a location filter, WHEN the location dropdown or selector is opened, THEN it shows only locations that have at least one published upcoming event (no empty location options).

**AC-12:** GIVEN an attendee has applied a filter, WHEN new events are published that match the filter, THEN those events appear the next time the listing is loaded or refreshed (filters operate on live data, not a snapshot).

## 3. Domain & Business Rules

Referenced from `ddd.md`:

- **VO-EventStatus:** Only `Published` events are searchable. Draft, Closed, and Cancelled events are excluded from search results, consistent with F-4.4.
- **VO-EventSchedule:** The start date is used for date range filtering. The date filter operates on the event's start date in the event's configured time zone.
- **VO-EventLocation:** Location filtering matches against the location value (city for physical events, "Online" for online events). The location filter uses the same representation shown in the listing (AC-03).
- **AGG-Event attributes searchable:** Title and description are searchable text fields for keyword matching. Location is a filterable field.
- **No new domain invariants:** Search and filter are read-only query concerns. They do not introduce new invariants or modify existing ones.
- **Transparent pricing (F-3.3):** Prices shown in filtered results are the same all-inclusive prices as in the unfiltered listing.
- **No-oversell (INV-10):** Sold-out events remain in search results. The sold-out indicator is shown as in the unfiltered listing.

## 4. UI Behavior & API Contract

### 4.1 Search and filter controls

| Control | Type | Behavior |
|---------|------|----------|
| **Keyword search** | Text input | Matches against event title and description (case-insensitive, partial). Debounced (300–500ms after typing stops). |
| **Date filter** | Dropdown / selector | Options: "Any date" (default), "Today", "Tomorrow", "This week", "This month", "Custom range" (start/end date pickers). |
| **Location filter** | Dropdown / selector | Options: "Any location" (default), plus distinct locations from published upcoming events. "Online" is a separate option. |
| **Clear / Reset** | Button | Resets all filters to defaults; URL query parameters are cleared. |

### 4.2 Filter interaction

- Filters are **combinational** — keyword + date + location narrow together (AND logic).
- The URL updates in real time as filters change (via browser history `pushState` or `replaceState`).
- Applying filters does not trigger a full page reload — the listing updates in place.
- The filter controls are visible above the event card grid, collapsed behind a toggle on mobile to save screen space.

### 4.3 API endpoint

The existing `GET /api/events` endpoint (from F-4.4) is extended with query parameters:

| Query param | Type | Description |
|-------------|------|-------------|
| `q` | `string` (optional) | Keyword to match against title and description |
| `date` | `string` (optional) | Predefined range: `today`, `tomorrow`, `this-week`, `this-month` |
| `dateFrom` | `string` (optional) | Custom date range start (ISO 8601 date, e.g., `2026-07-01`) — used when `date` is not set |
| `dateTo` | `string` (optional) | Custom date range end (ISO 8601 date) — used when `date` is not set |
| `location` | `string` (optional) | Location value to filter by (exact match against the event's location summary) |

All parameters are optional. Omitting a parameter means "no filter" for that dimension. The response shape is the same as F-4.4 — an array of event listing items.

A separate endpoint returns available locations for the filter dropdown:

| Aspect | Detail |
|--------|--------|
| **Method / path** | `GET /api/events/locations` |
| **Auth** | None required (public) |
| **Response** | Array of distinct location strings from published upcoming events |

### 4.4 Response shape

The `GET /api/events` response remains the same as F-4.4. No new fields are added. The filtering is purely server-side — the frontend receives only matching events.

### 4.5 Frontend routing

The `/events` route from F-4.4 is enhanced. No new routes are introduced. Query parameters drive the filter state.

## 5. Data & Storage Impact

- **PostgreSQL:** The search/filter query extends the existing listing query with `WHERE` clauses. Keyword search uses `ILIKE` (case-insensitive) on title and description columns. Date filtering uses the `StartDate` column. Location filtering uses the location summary column. An index on `(Status, StartDate)` supports the base query; additional indexes may be added if query performance requires it.
- **Redis:** The filtered listing response can be cached per filter combination. However, the number of possible filter combinations makes full caching impractical. A simpler approach: cache the unfiltered listing and apply filters in-memory, or skip caching for filtered results and rely on database query performance (acceptable at the project's intended scale — ASM-2).
- **MinIO:** No impact.
- **RabbitMQ:** No impact.

## 6. Real-Time & Consistency

- **Cache freshness:** If filtered results are cached, they follow the same eventual consistency as F-4.4 (30–60 second TTL). Unfiltered cached data can serve as the base for in-memory filtering.
- **No SignalR:** Search and filter do not require realtime push updates. The attendee refreshes or re-searches to see new events.
- **No integration events:** Search and filter are read-only queries — no domain or integration events are emitted.

## 7. Security & Privacy

- **Public data only:** Search and filter operate on publicly visible event metadata. No personal data or unpublished event details are exposed.
- **Input validation:** The `q` parameter is sanitized to prevent injection. Length limits apply (e.g., max 200 characters). Date parameters are validated as valid ISO 8601 dates. Location parameters are validated against known values.
- **No authentication required:** Search and filter are fully public, consistent with F-4.4.
- **Rate limiting consideration:** Search queries are more expensive than the unfiltered listing. Basic rate limiting or request throttling may be applied to prevent abuse, but is not a hard requirement at the project's scale.

## 8. Edge Cases

**EC-01:** An attendee searches for a keyword that matches no events. The listing shows the empty state message with a suggestion to adjust the search term or clear filters.

**EC-02:** An attendee selects "This week" but there are no events this week. The listing shows the empty state. The attendee can switch to "This month" or clear the date filter.

**EC-03:** An attendee searches for a keyword that matches a sold-out event. The sold-out event appears in results with its sold-out indicator.

**EC-04:** An attendee filters by a location, then an event in that location is cancelled. The event disappears from results on the next page load or refresh (consistent with AC-04 — only Published events appear).

**EC-05:** An attendee selects a custom date range where `dateFrom` is after `dateTo`. The API returns an empty result or a validation error (400/422).

**EC-06:** An attendee types a very long keyword (200+ characters). The API truncates or rejects with a clear message.

**EC-07:** An attendee filters by "Online" location. Only events whose location is marked as online appear.

**EC-08:** An attendee applies multiple filters and then shares the URL. The recipient sees the same filtered view (AC-07, AC-08).

**EC-09:** An attendee opens a shared URL with invalid filter parameters (e.g., `date=invalid`). The invalid parameters are ignored; the listing shows unfiltered results.

**EC-10:** An attendee is on a slow connection. The search debounce (AC-10) prevents excessive requests while typing.

## 9. Dependencies & Risks

**Dependencies:**
- F-4.4 (Public event listing) — the base listing page and API endpoint must exist.
- F-2.4 (Publish an event) — events must be publishable with status.
- F-3.1 (Define a ticket type) — ticket types with prices exist for the listing.

**Risks:**
- **R-01 (Low):** Keyword search performance — `ILIKE` on title and description could be slow with many events. Mitigation: at the intended scale (small events, ASM-2), this is acceptable. If needed, PostgreSQL full-text search can be added later.
- **R-02 (Low):** Location filter completeness — if events use inconsistent location formats (e.g., "HCMC" vs "Ho Chi Minh City"), the location filter may not match correctly. Mitigation: the location filter shows values derived from existing events, so it always matches what is stored.
- **R-03 (Low):** URL query parameter bloat — many filter combinations produce long URLs. Mitigation: query parameters are concise (`q`, `date`, `location`); custom date ranges use ISO 8601 dates.
- **R-04 (Low):** Mobile usability of filter controls — filters add UI complexity that could clutter the mobile experience. Mitigation: filters are collapsed behind a toggle on mobile; the keyword search input is always visible.

## 10. Assumptions

- Keyword search matches against event title and description only — not ticket type names, organizer names, or other fields.
- Date filtering operates on the event's start date, not end date. An event that starts this week but ends next week appears in "This week" results.
- The location filter uses the location summary shown in the listing (city or "Online"), not the full physical address.
- The "This week" filter uses the locale's week definition (Monday–Sunday or Sunday–Saturday, depending on the event's time zone or the server's locale).
- Search is case-insensitive and supports partial matches (e.g., "work" matches "Workshop").
- No category or tag taxonomy exists in the domain model, so category filtering is out of scope.
- Pagination is not introduced by this feature — the filtered results are returned as a single array, consistent with F-4.4.

## 11. Out of Scope

- **Category or tag filtering:** No category taxonomy exists in the domain model.
- **Autocomplete / search suggestions:** No typeahead or suggestion dropdown.
- **Search result ranking by relevance:** Results are sorted by start date (soonest first), same as the unfiltered listing.
- **Saved searches or alerts:** No "save this search" or "notify me about new events matching this filter."
- **Map-based filtering:** No geographic or map view.
- **Pagination or infinite scroll:** Not introduced by this feature.
- **Advanced text search (full-text, fuzzy):** Uses simple `ILIKE` matching; can be upgraded to PostgreSQL full-text search later if needed.
- **Filtering by price range:** Not included — the price is shown but not a filter dimension.
- **Filtering by ticket availability:** Not included — sold-out events remain visible.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should keyword search include organizer name or only event title/description? **Resolved:** Title and description only — organizer name is not shown on the public listing. | ✅ |
| 2 | Should the location filter use exact match or fuzzy match? **Resolved:** Exact match against the location summary derived from existing events (AC-11). | ✅ |
| 3 | Should the date filter include "Tomorrow" as a separate option? **Resolved:** Yes — it is a common quick filter for attendees looking for something soon. | ✅ |
| 4 | Should search results be sorted by relevance or by date? **Resolved:** By date (soonest first), consistent with the unfiltered listing. Relevance sorting adds complexity without clear value at this scale. | ✅ |
| 5 | Should the filter controls be always visible or collapsed on mobile? **Resolved:** Collapsed behind a toggle on mobile; keyword search input is always visible. | ✅ |
