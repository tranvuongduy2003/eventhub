---
artifact_type: spec
artifact_version: 1
id: spec-20260627100000-mobile-friendly-event-page
title: Mobile-friendly event page
slug: mobile-friendly-event-page
filename_template: 20260627100000-mobile-friendly-event-page.md
created_at: "2026-06-27T10:00:00+07:00"
updated_at: "2026-06-27T10:00:00+07:00"
status: draft
owner: product
tags: [spec, eventhub, ep-4-discovery]
feature_refs: [F-4.2]
ddd_refs: [BC-2]
prd_refs: [QG-4, G-4, ASM-3]
tech_refs: []
db_refs: [None]
github_issue: 51
search_index:
  keywords: [mobile, responsive, event page, phone, viewport, touch, attendee, public page, checkout, buy flow]
  bounded_contexts: [Event Management]
  user_personas: [PER-A1, PER-A2]
---

> GitHub: #51 (https://github.com/tranvuongduy2003/eventhub/issues/51)

# Feature: Mobile-friendly event page

> Features: F-4.2  |  Status: DRAFT  |  Date: 2026-06-27
> PRD: QG-4 (Mobile-friendly), G-4 (Make buying smooth on mobile), ASM-3 (Most attendees buy on a phone)  |  DDD: BC-2 (Event Management)  |  Tech: §7 (API surface)

## 1. Problem & Solution

**Problem:** The public event page (F-4.1) is the primary entry point for attendees. Most attendees discover and buy tickets on a phone (`product-requirements.md` ASM-3). If the page does not work well on a small screen — text too small, buttons hard to tap, horizontal scrolling required — attendees abandon the purchase. The current F-4.1 implementation focuses on desktop layout and content completeness; mobile experience is not yet addressed.

**Solution:** Adapt the existing public event page layout to be fully responsive on typical phone screens (320px–428px viewport width). The page must be readable without zooming, the buy flow must be usable with touch, and no horizontal scrolling should appear. This is a layout and styling effort on the existing F-4.1 page — no new data, endpoints, or domain logic.

**Personas:**
- **PER-A1** (General attendee) — discovers an event link on their phone and wants to read about it and buy tickets quickly.
- **PER-A2** (Group buyer) — buys multiple tickets on a phone for friends; needs the quantity selector and checkout to work comfortably with touch.

**Scope:**
- **In scope:** F-4.2 — responsive layout for the public event page on phone-sized screens.
- **Out of scope:** Rich link previews (F-4.3), public event listing (F-4.4), search (F-4.5), offline support, native app behavior, tablet-specific layouts, landscape orientation optimization.

## 2. Acceptance Criteria

**AC-01:** GIVEN a published event page (F-4.1) WHEN a visitor opens it on a phone (viewport width ≤ 428px) THEN the page displays without horizontal scrolling — all content fits within the screen width.

**AC-02:** GIVEN the event page on a phone WHEN the visitor reads the event title, description, date/time, and location THEN the text is legible at default zoom without requiring the visitor to zoom in (minimum 16px body text, sufficient contrast).

**AC-03:** GIVEN the event page on a phone WHEN the visitor views the cover image THEN it scales to fit the screen width, maintaining its aspect ratio, without overflowing or requiring horizontal scroll.

**AC-04:** GIVEN the event page on a phone WHEN the visitor views the ticket type list THEN each ticket type name, price, and availability status is clearly readable and distinguishable on a single column layout.

**AC-05:** GIVEN the event page on a phone WHEN the visitor taps the buy/CTA button or the ticket quantity selector THEN the interactive element is large enough to tap comfortably (minimum 44×44px touch target per WCAG 2.1 AA) and triggers the expected action.

**AC-06:** GIVEN the event page on a phone WHEN the visitor scrolls through the page THEN the layout is a single column, content sections stack vertically, and no side-by-side elements overflow the viewport.

**AC-07:** GIVEN a draft, closed, or cancelled event WHEN a visitor opens the page on a phone THEN the appropriate state message is displayed responsively (same mobile layout rules as the published page).

**AC-08:** GIVEN the event page on a phone WHEN the visitor scrolls through the page THEN the buy CTA remains visible as a sticky element at the bottom of the viewport at all times, providing a persistent entry point to the checkout flow (EP-5).

**AC-09:** GIVEN the event page on a phone WHEN the description is long THEN it is collapsed by default showing a preview, with a "Read more" toggle to expand and "Show less" to collapse again.

**AC-10:** GIVEN the event page on a phone WHEN the visitor uses the page THEN the experience meets WCAG 2.1 AA baseline — focus visible on interactive elements, sufficient color contrast, and labels on any form controls.

**AC-11:** GIVEN the event page on a desktop or tablet screen WHEN a visitor opens it THEN the existing desktop layout is preserved — the mobile changes do not degrade the wide-screen experience.

## 3. Domain & Business Rules

This feature has no domain or business rule impact. It is a pure presentation-layer concern on the existing public event page (F-4.1).

The page displays data already defined in BC-2 (Event Management):
- `AGG-Event` attributes: title, description, schedule, location, cover image, status.
- `ENT-TicketType` attributes: name, price (`VO-Money`), availability (capacity − reserved − sold).
- Event status lifecycle (`VO-EventStatus`): Draft, Published, Closed, Cancelled — each shows an appropriate state on the page.

No new invariants, domain events, or aggregates are introduced.

## 4. UI Behavior

### 4.1 Layout approach

The page uses a **mobile-first responsive layout**:
- **Base layout:** single column, full-width, designed for phone screens (320px–428px).
- **Breakpoints:** the layout widens gracefully at larger breakpoints (tablet, desktop) — the existing desktop layout is adapted, not replaced.
- **Content order on mobile:** cover image at top, followed by title, date/time, location, description, ticket types, and buy CTA — in a logical reading order that puts the most actionable content (tickets + buy) within easy reach.

### 4.2 Key component behavior on phone

| Component | Mobile behavior |
|-----------|----------------|
| Cover image | Full-width, aspect ratio preserved, no horizontal overflow |
| Event title | Prominent, readable at default zoom |
| Date/time + location | Stacked below title, icons or labels for quick scanning |
| Description | Full-width text; **collapsible on mobile** — shows a preview (e.g., first 3–4 lines) with a "Read more" / "Show less" toggle to reduce scroll depth |
| Ticket type list | Single column, each type as a card/row with name, price, and availability |
| Buy CTA | Prominent, full-width, **sticky at the bottom of the viewport on mobile** — always visible while scrolling |
| Quantity selector | Touch-friendly controls (buttons, not tiny dropdowns) |
| Event state banner | Full-width banner for Draft/Closed/Cancelled states |

### 4.3 Touch targets

All interactive elements (buttons, links, quantity selectors, ticket type selection) meet a minimum 44×44px touch target area, per WCAG 2.1 AA and Apple Human Interface Guidelines. Spacing between adjacent interactive elements is sufficient to prevent accidental taps.

### 4.4 No horizontal scrolling

The page and all its content (including any embedded images, long text, or ticket type cards) fit within the viewport width at any phone size. No element causes horizontal overflow.

## 5. Data & Storage Impact

None. This feature is a presentation-layer change only. No new database tables, columns, API endpoints, or storage objects are required. The page consumes the same public event data API (F-4.1).

## 6. Real-Time & Consistency

N/A. No real-time or consistency concerns. The page reads published event data; any live updates (F-11.1) are a separate feature.

## 7. Security & Privacy

No new security or privacy concerns. The page remains publicly accessible for published events (same as F-4.1). No new data is exposed. Guest visitors continue to see the page without authentication.

## 8. Edge Cases

**EC-01:** Very long event title or description on a small screen — the title wraps within the container without overflow; the description is collapsed by default (showing a preview) with a "Read more" toggle to expand the full text.

**EC-02:** A ticket type with a long name (e.g., "Early Bird Weekend Pass with Workshop Access") — the name wraps or truncates gracefully (e.g., ellipsis) while keeping the price and buy action visible.

**EC-03:** An event with many ticket types (e.g., 5+) — the list scrolls vertically within the page; all types remain accessible without horizontal overflow.

**EC-04:** An event with a very wide or very tall cover image — the image scales to fit the screen width with consistent aspect ratio; tall images do not push the buy CTA excessively far below the fold.

**EC-05:** An event in Draft or Cancelled status opened on a phone — the state message is displayed in the same responsive layout; no buy CTA is shown.

**EC-06:** Visitor on an extremely narrow viewport (< 320px, e.g., some older phones) — the page remains functional, though not optimized for these devices. Content is still readable and no broken layout occurs.

**EC-07:** Visitor rotates phone to landscape — the page remains usable, though portrait is the primary orientation. No critical content is hidden or broken in landscape.

## 9. Dependencies & Risks

**Dependencies:**
- F-4.1 (Shareable public event page) — must exist. This feature adapts the existing page.
- F-3.1 (Define a ticket type) — the page displays ticket types; they must exist in the system.
- F-3.3 (Transparent pricing) — prices shown must be final/all-inclusive.

**Risks:**
- **R-01:** Existing desktop layout may be tightly coupled, requiring significant refactoring to become responsive. Mitigation: use a mobile-first approach; adapt, don't rewrite.
- **R-02:** Touch target sizes on existing components may be below the 44px minimum. Mitigation: audit and increase tap areas during implementation.
- **R-03:** Long descriptions or many ticket types may create excessive scroll on mobile. Mitigation: descriptions are collapsible on mobile (see AC-09); ticket type list scrolls vertically.

## 10. Assumptions

- The F-4.1 public event page already exists and is functional on desktop.
- The frontend uses Tailwind CSS (per `design-system.md`), which supports responsive utilities out of the box.
- The primary mobile browser target is modern mobile Safari and Chrome (last 2 versions).
- Portrait orientation is the primary mobile use case; landscape is secondary.
- The page does not need to work as a PWA or offline for this feature.
- No minimum viewport width below 320px — the page remains functional but is not optimized below that threshold.

## 11. Out of Scope

- Rich link previews (F-4.3) — social/OG meta tags, not a layout concern.
- Public event listing page (F-4.4) — a separate page, not this feature.
- Search and filter (F-4.5) — separate feature.
- Tablet-specific layouts — tablets inherit the responsive layout but no tablet-optimized design is required.
- Native app or PWA behavior.
- Accessibility beyond WCAG 2.1 AA baseline (e.g., screen reader optimization for the full page is a later concern).
- Performance optimization beyond what Tailwind/Vite provide by default (e.g., image CDN, lazy loading) — these are infrastructure concerns.

## 12. Open Questions

| # | Question | Answer |
|---|----------|--------|
| 1 | Should the buy CTA be sticky at the bottom of the viewport on mobile (always visible while scrolling)? | ✅ Yes — sticky CTA at bottom of viewport on mobile. |
| 2 | Should long event descriptions be collapsible on mobile to reduce scroll depth? | ✅ Yes — collapse long descriptions on mobile with a "Read more" toggle. |
| 3 | Is there a minimum supported viewport width below 320px? | ✅ No minimum below 320px — page should remain functional but not optimized below that. |
