---
artifact_type: spec
artifact_version: 1
id: spec-20260625150117-transparent-all-inclusive-pricing
title: Transparent, All-Inclusive Pricing
slug: transparent-all-inclusive-pricing
filename_template: 20260625150117-transparent-all-inclusive-pricing.md
created_at: "2026-06-25T15:01:17Z"
updated_at: "2026-06-25T15:01:17Z"
status: draft
plan_ready: true
owner: product
tags: [spec, eventhub, ticketing]
feature_refs: [F-3.3]
ddd_refs: [BC-2, BC-3, AGG-Event, AGG-Order, VO-Money, INV-13, INV-20, INV-25]
prd_refs: [DEC-1, QG-2, QG-3]
tech_refs: [Tech §5, Tech §6]
db_refs: [Tech §6]
github_issue: 38
search_index:
  keywords: [pricing, transparent, fees, all-inclusive, checkout, price, money, total]
  bounded_contexts: [Event Management, Sales]
  user_personas: [PER-A1, PER-A2]
---

> GitHub: [#38](https://github.com/tranvuongduy2003/eventhub/issues/38)

# Feature: Transparent, All-Inclusive Pricing

> Features: F-3.3  |  Status: DRAFT  |  Date: 2026-06-25
> PRD: DEC-1, QG-2, QG-3  |  DDD: BC-2, BC-3, VO-Money, INV-25  |  Tech: §5, §6

## 1. Problem & Solution

**Problem:** Attendees on many ticketing platforms see a price on the event page, only to discover additional service fees, processing fees, or convenience charges at the final checkout step. This erodes trust and creates a negative purchase experience. The price they expected is rarely the price they pay.

**Solution:** EventHub guarantees that the price an attendee sees at every touchpoint — the public event page, the ticket selection screen, and the checkout summary — is the exact amount they will be charged. No platform fee, service fee, or hidden charge is ever added. The organizer sets a price; the attendee pays that price. This is a core product principle (`product-requirements.md` DEC-1, QG-2) and a key differentiator.

**Personas:**
- **PER-A1** (General attendee) — wants a clear, honest price with no surprises.
- **PER-A2** (Group buyer) — buying multiple tickets; needs to trust the total before committing.

**Scope:** F-3.3 is the pricing transparency guarantee. It touches the event page (EP-4), checkout summary (F-5.4), and the order total calculation. Discount codes (F-3.7) are out of scope for this spec but, when present, must also be reflected transparently.

## 2. Acceptance Criteria

**AC-01:** GIVEN a published event with ticket types priced by the organizer, WHEN an attendee views the public event page (EP-4), THEN each ticket type displays its final price — the exact amount that will be charged per ticket, with no additional fees or surcharges listed or implied.

**AC-02:** GIVEN an attendee has selected tickets and reached the checkout summary (F-5.4), WHEN they review the order, THEN the line items show the same per-ticket price as the event page, and the total equals the sum of line items — no platform fee, processing fee, or other charge is added.

**AC-03:** GIVEN an attendee completes a purchase for a paid order, WHEN the payment is processed, THEN the amount charged to their payment method equals the total displayed in the checkout summary — no more, no less.

**AC-04:** GIVEN an attendee completes a purchase for a free ticket (price = 0), WHEN the order is placed, THEN no payment step is presented and the order is confirmed immediately, consistent with F-3.2 and F-6.2.

**AC-05:** GIVEN the organizer changes a ticket type's price after an attendee has already placed an order, WHEN that attendee's order is fulfilled, THEN the order total reflects the price at the time of order placement, not the current price — price is snapshotted at order creation.

**AC-06:** GIVEN any point in the purchase flow (event page, checkout summary, payment confirmation), WHEN the system calculates or displays a price, THEN the currency is the single configured currency for the platform, and the amount is non-negative.

**AC-07:** GIVEN EventHub's business model (DEC-1), WHEN any price or total is displayed or charged, THEN no EventHub platform fee is added at any step — the organizer receives the full ticket price through the payment provider.

## 3. Domain & Business Rules

**Price origin:** The organizer sets a price on each ticket type when defining it (F-3.1). The price is stored as a `VO-Money` value object (amount + currency) on the `ENT-TicketType` entity within the `AGG-Event` aggregate (BC-2). The price must be non-negative (`INV-13`).

**No platform fee:** EventHub's business model is "no fee at launch" (`product-requirements.md` DEC-1). The system does not calculate, store, or display any platform fee. The total an attendee sees is the total the organizer receives (minus any payment provider fees, which are between the organizer and the provider — invisible to the attendee).

**Price snapshot at order placement:** When an order is placed (`AGG-Order`, BC-3), each order line captures a `VO-Money UnitPriceSnapshot` — the price of the ticket type at the moment of placement (`INV-25`). This ensures that even if the organizer later changes a ticket type's price, existing orders are unaffected. The order total is computed from these snapshots (`INV-20`: `Total = Σ line totals`).

**Price consistency guarantee:** The same price must appear at every stage of the purchase journey:
1. Public event page (EP-4) — reads from `ENT-TicketType.Price` on the `AGG-Event`.
2. Checkout summary (F-5.4) — reads from the snapshotted price on the `AGG-Order` line.
3. Payment amount — equals the order total.

If the price changes between the attendee viewing the event page and placing the order, the order captures whatever the price was at placement time. The system does not need to warn about mid-session price changes (out of scope; this is a small-event platform).

**Currency:** A single configured currency is used across the platform (`VO-Money`). Multi-currency is a non-goal (`product-requirements.md` §6.2).

## 4. UI Behavior

**Public event page (EP-4):**
- Each ticket type row shows: name, final price (e.g., "50,000 ₫"), and availability.
- No "fees added at checkout" notice. No asterisks. No "from X" phrasing — the price shown is the price.

**Checkout summary (F-5.4):**
- Line items: ticket type name × quantity = unit price × quantity.
- Total: sum of line items.
- No "service fee," "processing fee," or "convenience fee" line — these do not exist in EventHub.
- For free tickets: total shows "0 ₫" (or equivalent) and no payment step is presented.

**Order confirmation:**
- Displays the total that was charged, matching the checkout summary.

**Error states:**
- If a price cannot be determined (e.g., ticket type deleted between selection and checkout), the checkout is blocked with a clear message asking the attendee to return to the event page.

## 5. Data & Storage Impact

**PostgreSQL (authoritative):**
- `ENT-TicketType.Price` — the current price set by the organizer, stored as `VO-Money` (amount in minor units + currency code). Already defined in F-3.1.
- `ENT-OrderLine.UnitPriceSnapshot` — the price at order placement. Already defined in the Order aggregate.
- No new tables or columns required for F-3.3 — this feature is a **consistency guarantee** enforced by the existing data model.

**Redis:** No caching of prices for the write path. Read-side caching of event page data (ticket types + prices) is permitted since it is rebuildable from PostgreSQL.

**RabbitMQ:** No new integration events. Price transparency is enforced at the point of display and calculation, not through messaging.

## 6. Real-Time & Consistency

**N/A for realtime.** F-3.3 does not require SignalR or live updates. Price changes by the organizer are reflected on the next page load.

**Consistency:** Price snapshotting (`INV-25`) ensures that an order's total is immutable once placed, regardless of subsequent price changes. This is enforced within the `AGG-Order` aggregate boundary.

## 7. Security & Privacy

**No special security concerns.** Prices are public data (displayed on the public event page). No payment credentials are stored by EventHub (`product-requirements.md` QG-6, DEC-1).

**Guest checkout:** Price transparency applies equally to guest attendees (no account required). The price displayed and charged is the same regardless of authentication state.

## 8. Edge Cases

**EC-01:** Organizer raises a ticket price while an attendee is mid-checkout. The order captures the price at placement time. If the attendee returns to the event page after placing, they see the new price — but their confirmed order is unaffected.

**EC-02:** Organizer lowers a ticket price after some orders are placed. Existing orders retain the higher snapshotted price. The organizer may choose to refund the difference manually (out of scope for F-3.3).

**EC-03:** A ticket type's price is set to zero (free). No payment step is shown. The order total is zero and it is confirmed immediately (per F-3.2, F-6.2).

**EC-04:** The event page is loaded in a browser that caches aggressively. The price displayed may be stale. This is acceptable — the order always captures the authoritative price from the database at placement time.

**EC-05:** Two different ticket types on the same event have different prices. Each line item in the checkout summary shows its own unit price independently. The total is the sum.

**EC-06:** A discount code (F-3.7, out of scope for this spec) is applied. The discounted total replaces the pre-discount total as the amount to be charged. The discount is shown transparently as a line item reduction, not as a hidden adjustment.

## 9. Dependencies & Risks

**Dependencies:**
- **F-3.1** (Define a ticket type) — must exist; the price originates here.
- **F-3.2** (Free tickets) — free ticket behavior (zero price → no payment step) is a prerequisite.
- **EP-4** (Public event page) — where the price is first displayed to the attendee.
- **F-5.4** (Final price summary) — where the price is confirmed before payment.
- **F-6.2** (Free-order auto-confirm) — zero-total order behavior.

**Risks:**
- **R-01:** If price snapshotting is not correctly implemented in the Order aggregate, post-placement price changes could affect confirmed orders. Mitigation: `INV-25` is a core invariant; verify via domain unit tests.
- **R-02:** If the checkout summary reads the current ticket type price instead of the snapshotted order line price, a price change between placement and confirmation could cause a mismatch. Mitigation: checkout summary reads from the Order, not from the Event.

## 10. Assumptions

1. A single currency is configured for the platform. Multi-currency support is not needed.
2. The payment provider charges the exact amount EventHub requests — no provider-side adjustments are visible to the attendee.
3. Price changes by the organizer take effect immediately on the next page load (no approval workflow or delay).
4. The "all-inclusive" guarantee means the attendee pays exactly the displayed price. Any payment provider fees are between the provider and the organizer, not surfaced to the attendee.

## 11. Out of Scope

- **F-3.7** (Discount codes) — reduces the total transparently but is a separate feature.
- **F-3.8** (Scheduled on-sale window) — may affect when a ticket type is purchasable, not its price.
- **Price change notifications** to attendees — not required for MVP.
- **Multi-currency** — non-goal (`product-requirements.md` §6.2).
- **Tax display** — may be needed locally (`product-requirements.md` DEP-3) but is out of scope for F-3.3.
- **Refund amount calculations** — handled by F-6.6 and the payment provider.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the event page show a "no hidden fees" badge or trust indicator to reinforce the all-inclusive guarantee? | ✅ Yes — "All-inclusive — no hidden fees" badge on ticket list and checkout summary |
| 2 | If a tax-compliant receipt mechanism is added later (DEP-3), should the tax amount be shown separately or remain included in the displayed price? | ✅ Tax remains included in the displayed price. A "Price includes applicable taxes" note is shown. Tax is never broken out separately — the all-inclusive guarantee means the attendee sees one price, period. |
