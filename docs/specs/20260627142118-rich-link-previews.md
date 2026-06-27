---
artifact_type: spec
artifact_version: 1
id: spec-20260627142118-rich-link-previews
title: Rich link previews
slug: rich-link-previews
filename_template: 20260627142118-rich-link-previews.md
created_at: "2026-06-27T14:21:18Z"
updated_at: "2026-06-27T14:21:18Z"
status: draft
owner: product
tags: [spec, eventhub, event-discovery]
feature_refs: ["F-4.3"]
ddd_refs: ["BC-2", "AGG-Event", "VO-Slug", "VO-CoverImageRef"]
prd_refs: ["QG-1", "QG-2", "QG-4"]
tech_refs: ["Tech §5", "Tech §7"]
db_refs: ["None"]
github_issue: 53
search_index:
  keywords: [rich, link, preview, opengraph, og, twitter, card, social, share, meta, tags, crawler]
  bounded_contexts: ["Event Management"]
  user_personas: ["PER-O1", "PER-O2"]
---

> GitHub: #53 (https://github.com/tranvuongduy2003/rebuilt-eventhub/issues/53)

# Feature: Rich link previews

> Features: F-4.3  |  Status: DRAFT  |  Date: 2026-06-27
> PRD: QG-1 (simplicity), QG-2 (transparency), QG-4 (mobile-friendly)  |  DDD: BC-2 AGG-Event, VO-Slug, VO-CoverImageRef  |  Tech: §5, §7

## 1. Problem & Solution

**Problem:** An organizer shares their event link on social media (Facebook, Twitter/X, LinkedIn), chat apps (WhatsApp, Telegram, Messenger), or other platforms. Without rich link previews, the shared link appears as a bare URL or with a generic placeholder — no event title, no cover image, no date. This looks unprofessional and gives potential attendees no reason to click through. The organizer's ability to promote their event through casual sharing is severely diminished.

**Solution:** When a link to a published event page is shared, the platform serves Open Graph (OG) and Twitter Card meta tags that describe the event. Social platforms and chat apps crawl these tags and render a rich preview card showing the event title, cover image, and date. The preview is generated server-side so that crawler user-agents (which typically do not execute JavaScript) receive the meta tags in the initial HTML response.

**Personas:**
- **PER-O1** (Individual organizer) — shares their event link on personal social media to attract attendees.
- **PER-O2** (Small group organizer) — shares event links in group chats, community pages, and messaging apps to promote their event.

**Scope:**
- **In scope:** F-4.3 — Open Graph and Twitter Card meta tags for published event pages, served to crawler user-agents.
- **Out of scope:** SEO optimization beyond OG tags (structured data, sitemap), social sharing buttons or share UI, analytics on link clicks, custom preview images per platform, dynamic preview rendering for non-published events.

## 2. Acceptance Criteria

**AC-01:** GIVEN a published event with a cover image, WHEN a social platform or chat app crawls the event page URL (`/events/{slug}`), THEN the response includes Open Graph meta tags with `og:title` (event title), `og:image` (cover image URL), `og:description` (event date/time + location summary), and `og:type` set to `event`.

**AC-02:** GIVEN a published event without a cover image, WHEN a social platform crawls the event page URL, THEN the response includes Open Graph meta tags with `og:title` and `og:description`, and `og:image` is omitted (not set to a broken or placeholder URL).

**AC-03:** GIVEN a published event, WHEN the event link is shared on Twitter/X, THEN the response includes Twitter Card meta tags (`twitter:card` set to `summary_large_image` when a cover image exists, or `summary` when no image exists) with `twitter:title` and `twitter:description`.

**AC-04:** GIVEN a published event, WHEN a crawler fetches the event page URL, THEN the `og:title` matches the event title exactly, the `og:image` is a fully qualified URL (not a relative path) pointing to the cover image, and the `og:description` contains the event start date and location in a human-readable format.

**AC-05:** GIVEN a Draft, Closed, or Cancelled event (or a non-existent slug), WHEN a crawler fetches the URL, THEN no event-specific OG tags are served — the page either returns generic/no OG tags or returns a 404, preventing stale or incorrect previews for non-published events.

**AC-06:** GIVEN a published event whose title, description, or cover image is updated by the organizer, WHEN the event link is shared after the update, THEN the preview reflects the updated information (within the platform's caching window — typically 24–48 hours, or instantly via platform-specific cache-bust URLs).

**AC-07:** GIVEN the event page is loaded by a regular browser user (not a crawler), WHEN the page renders, THEN the OG meta tags are present in the HTML `<head>` so that browser extensions and tools that read OG tags also work correctly.

**AC-08:** GIVEN a published event with a very long title (over 100 characters), WHEN the OG tags are served, THEN `og:title` contains the full title (OG has no hard limit, but platforms truncate at display time) and `og:description` provides a concise summary suitable for preview cards.

## 3. Domain & Business Rules

Referenced from `ddd.md`:

- **VO-Slug:** The stable, URL-safe identifier for the public event page. The rich preview targets the same URL as F-4.1: `/events/{slug}`. The slug does not change after publishing, so preview links remain stable.
- **VO-CoverImageRef:** The object reference for the event's cover image in MinIO. The `og:image` tag must use a fully qualified URL that resolves to the cover image. If no cover image is set, `og:image` is omitted.
- **VO-EventStatus:** Only `Published` events serve rich previews. Draft, Closed, and Cancelled events do not serve event-specific OG tags — this prevents stale or misleading previews for events that are not publicly available for purchase.
- **AGG-Event attributes used:** title, description, start date/time, location, cover image, status. No new attributes or invariants are introduced.
- **Transparent pricing (F-3.3):** The preview description can mention the starting price or that tickets are available, but the primary preview content is title + image + date — not pricing. Pricing is for the page itself, not the preview card.

## 4. UI Behavior & API Contract

### 4.1 Meta tags served

The following meta tags are injected into the HTML `<head>` of the event page when the event is published:

**Open Graph (og:):**
| Tag | Value | Notes |
|-----|-------|-------|
| `og:title` | Event title | From `AGG-Event` title |
| `og:description` | Date + location summary (no pricing) | e.g., "Sat, Jun 28, 2026, 7:00 PM · Ho Chi Minh City" |
| `og:image` | Cover image URL | Fully qualified URL to MinIO object; omitted if no cover |
| `og:image:width` | Image width | Optional; helps platforms render without flicker |
| `og:image:height` | Image height | Optional |
| `og:image:alt` | "Cover image for {event title}" | Accessibility |
| `og:type` | `event` | Standard OG type for events |
| `og:url` | Canonical event page URL | `https://{host}/events/{slug}` |
| `og:site_name` | "EventHub" | Platform name |

**Twitter Card:**
| Tag | Value | Notes |
|-----|-------|-------|
| `twitter:card` | `summary_large_image` (with image) or `summary` (without) | Determines card layout |
| `twitter:title` | Event title | Same as `og:title` |
| `twitter:description` | Date + location summary | Same as `og:description` |
| `twitter:image` | Cover image URL | Same as `og:image`; omitted if no cover |

### 4.2 Server-side rendering approach

The EventHub frontend is a React SPA (Vite). Social media crawlers and chat apps typically do not execute JavaScript — they read only the raw HTML response. Therefore, OG tags must be present in the initial HTML response, not injected client-side.

**Approach: Vite SSR (server-side rendering).** The Vite frontend uses SSR to render the event page on the server, producing HTML that includes the OG and Twitter Card meta tags in `<head>`. Regular browser users receive the same server-rendered HTML and then the React app hydrates on the client. This ensures:
1. Crawlers and chat apps see OG tags in the raw HTML response.
2. Browser extensions and tools that read OG tags also work (AC-07).
3. A single rendering path — no separate API-side HTML generation needed.

### 4.3 Crawler detection

Major crawlers identify themselves via the `User-Agent` header:
- Facebook: `facebookexternalhit`
- Twitter/X: `Twitterbot`
- LinkedIn: `LinkedInBot`
- WhatsApp: `WhatsApp`
- Telegram: `TelegramBot`
- Slack: `Slackbot-LinkExpanding`
- Discord: `Discordbot`
- Google: `Googlebot`

The implementation should serve OG tags to **all requests** (not just crawlers) to keep the logic simple and to support browser extensions and tools that read OG tags (AC-07).

## 5. Data & Storage Impact

- **PostgreSQL:** No changes. The OG tag data (title, description, start date, location, cover image URL) is already available from the public event query (F-4.1). No new columns or tables.
- **Redis:** The OG tag HTML can be cached alongside the public event data (same cache key pattern). A 30–60 second TTL is sufficient.
- **MinIO:** The cover image URL is already stored as `VO-CoverImageRef`. The `og:image` tag uses the same URL. No new objects or references.
- **RabbitMQ:** No impact — this is a read-only concern.

## 6. Real-Time & Consistency

- **Cache freshness:** When an organizer updates the event title, description, or cover image, the OG tags reflect the change within the cache TTL (30–60 seconds). Social platforms cache preview data for 24–48 hours after their first crawl. The organizer can use platform-specific cache refresh tools (Facebook Sharing Debugger, Twitter Card Validator) to force an immediate re-crawl.
- **No SignalR:** This feature has no realtime push requirement.
- **No integration events:** OG tags are a read-only presentation concern — no domain or integration events are emitted.

## 7. Security & Privacy

- **Public data only:** OG tags contain only publicly visible event metadata (title, date, location, cover image). No personal data, attendee information, or payment details are included.
- **Draft event protection:** Draft events do not serve OG tags (AC-05). This prevents unpublished event details from being previewed if someone guesses or leaks a slug before publishing.
- **Image URLs:** The cover image URL in `og:image` is publicly accessible (same as F-4.1). No signed URLs or authentication is needed for preview images.
- **No new attack surface:** The OG tag rendering is a read-only enhancement to an existing public endpoint. It does not introduce new user input vectors.

## 8. Edge Cases

**EC-01:** An event has a cover image that is very small (e.g., 100×100px). Social platforms recommend at least 1200×630px for `og:image`. The tag is still served — the platform renders it, though quality may be low. No server-side validation of image dimensions is required; the organizer is responsible for uploading a suitable image.

**EC-02:** An event has a cover image with a non-standard aspect ratio (e.g., very tall portrait). The preview card crops or letterboxes the image per the platform's rendering rules. EventHub does not transform or resize the image for preview purposes.

**EC-03:** The cover image URL in MinIO becomes temporarily unavailable (e.g., MinIO downtime). The `og:image` tag still contains the URL — the social platform shows a broken image or falls back to no image. The event title and description still appear.

**EC-04:** An organizer changes the event title after the link has already been shared and crawled. The old preview persists in the platform's cache until it expires or is manually refreshed. This is expected behavior and not a defect.

**EC-05:** The event slug contains special characters or unicode. Since slugs are URL-safe by design (`VO-Slug`), this should not occur. If it does, the `og:url` tag uses the properly encoded URL.

**EC-06:** A very long event title (200+ characters). The `og:title` tag contains the full title. Social platforms truncate at display time — this is the platform's behavior, not EventHub's responsibility.

**EC-07:** The event has no description. The `og:description` falls back to date + location only, or a generic "Event on EventHub" message. The tag is always present (never empty).

**EC-08:** A crawler requests the URL for a non-existent slug. The response is a 404 with no event-specific OG tags (AC-05).

## 9. Dependencies & Risks

**Dependencies:**
- F-4.1 (Shareable public event page) — the public event page and its API endpoint must exist. The OG tags are an enhancement to this page.
- F-2.2 (Add an event cover image) — the cover image must be stored in MinIO for `og:image` to have a valid URL.
- F-2.4 (Publish an event) — the event must be published with a slug for the public URL to exist.

**Risks:**
- **R-01 (Low):** SPA rendering — if OG tags are injected client-side only, crawlers will not see them. Mitigation: serve OG tags in the initial HTML response (server-side approach).
- **R-02 (Low):** Social platform cache staleness — platforms cache preview data for 24–48 hours. After an event update, old previews may persist. Mitigation: document that organizers can use platform-specific debug tools to refresh.
- **R-03 (Low):** Image format compatibility — some platforms may not support certain image formats (e.g., WebP). Mitigation: MinIO serves images with standard MIME types; the cover upload process should accept common formats (JPEG, PNG).

## 10. Assumptions

- The public event page URL is `/events/{slug}` (established in F-4.1).
- The cover image URL from MinIO is publicly accessible without authentication.
- Social platforms and chat apps are the primary consumers of OG tags; SEO-focused structured data (JSON-LD) is out of scope.
- The single configured currency is used if pricing is mentioned in the preview description (no multi-currency — `prd.md` §6.2).
- The API or frontend can detect crawler user-agents, or OG tags are served to all requests (preferred — simpler, supports browser extensions).
- The OG tag implementation does not require a separate rendering service — it is handled within the existing API or Vite SSR capability.

## 11. Out of Scope

- **SEO beyond OG tags:** JSON-LD structured data, sitemap generation, robots.txt optimization, canonical URL management beyond the `og:url` tag.
- **Social sharing buttons:** "Share on Facebook/Twitter" UI components on the event page. The organizer copies and shares the URL manually.
- **Analytics:** Tracking how many times the link was shared, clicked, or previewed.
- **Custom preview images per platform:** A single cover image is used for all platforms. No per-platform image sizing or cropping.
- **Preview for non-published events:** Draft, Closed, and Cancelled events do not serve OG tags.
- **Cache-bust or instant refresh mechanism:** No API endpoint to force social platforms to re-crawl. Organizers use platform-specific debug tools.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should OG tags be served to all requests (simpler) or only to crawler user-agents (slightly more complex but avoids unnecessary HTML for regular browsers)? **Resolved:** Serve to all requests — simpler implementation, supports browser extensions and tools. | ✅ |
| 2 | Should `og:description` include ticket pricing information (e.g., "From 150,000₫") or only date + location? **Resolved:** Only date and location — pricing is for the page itself, not the preview card. | ✅ |
| 3 | Should the implementation use server-side rendering (Vite SSR) or API-side HTML generation for crawler responses? **Resolved:** SSR (Vite server-side rendering). | ✅ |
