---
document_type: feature_specification
product_name: EventHub
version: "1.0"
status: draft
last_updated: "2026-06-14"
owner: Builder (solo / pet project)
language: en
companion_documents:
  product: product-requirements.md
  technical: technical-design.md
identifier_scheme:
  epic: "EP-<n>"
  feature: "F-<epic>.<n>"
phases: [MVP, Next, Later]
---

# EventHub — Feature Specification (`feature-specification.md`)

**What EventHub does, feature by feature.**

---

## 0. About this document (read first)

### 0.1 Purpose
This document defines **the capabilities of EventHub** — the epics, the features inside them, who each serves, and how each is judged done. It is the *what*. The *why/who/scope* lives in [`product-requirements.md`](product-requirements.md); the *how it is built* lives in [`technical-design.md`](technical-design.md).

### 0.2 How to read this document (for humans and AI agents)
- **Identifiers.** Epics are `EP-<n>`. Features are `F-<epic>.<n>` (e.g., `F-3.4`). Both are stable and referenceable.
- **Each feature carries a one-line header:** `[Phase] · serves <persona(s)> · depends on <feature id(s)>`. The `depends on` field is the backbone of the document: a feature may only be built after everything it depends on exists.
- **Phases** describe when a feature is built, not a calendar date (the project is milestone-based — see `product-requirements.md` DEC-5):
  - **MVP** — the smallest end-to-end product: run one small event from creation to entry (per `product-requirements.md` DEC-3).
  - **Next** — richer organizing, discovery, audience tools, and fair transfer.
  - **Later** — messaging, returns, and enhancements.
- **Personas** (defined in `product-requirements.md` §4): `PER-O1` individual organizer · `PER-O2` small group / club / small-business organizer · `PER-A1` general attendee · `PER-A2` group buyer.
- **Glossary** of shared terms is in `product-requirements.md` §12 and is not repeated here.
- **Acceptance criteria** state the observable behavior that marks a feature complete. They are intentionally implementation-free; mechanisms (aggregates, ports, infrastructure) are in `technical-design.md`.

### 0.3 Build spine (how the epics build on each other)
Each epic depends on the ones before it. The core path is a single chain, and the last three epics build on the data and tickets the chain produces:

> **EP-1 Accounts → EP-2 Events → EP-3 Ticketing → EP-4 Discovery → EP-5 Purchase → EP-6 Payment → EP-7 Delivery → EP-8 Check-in**, then **EP-9 Audience & Results**, **EP-10 Transfer & Returns**, and **EP-11 Realtime** build on top.

You cannot sell a ticket before an event exists; you cannot price a ticket before a ticket type exists; you cannot check in a ticket before one is issued; you cannot report on attendance before check-in produces it. The dependency fields make this explicit at the feature level.

### 0.4 Epic index

| Epic | Title | Phase span | Depends on |
|------|-------|-----------|------------|
| EP-1 | Organizer Accounts & Identity | MVP → Later | — |
| EP-2 | Event Creation & Management | MVP → Next | EP-1 |
| EP-3 | Ticketing & Transparent Pricing | MVP → Next | EP-2 |
| EP-4 | Event Discovery & Access | MVP → Next | EP-2, EP-3 |
| EP-5 | Purchase & Checkout | MVP | EP-4, EP-3 |
| EP-6 | Payment | MVP → Next | EP-5 |
| EP-7 | Ticket Delivery & Attendee Access | MVP → Later | EP-6 |
| EP-8 | Event-Day Check-in | MVP → Later | EP-7, F-1.7 |
| EP-9 | Audience & Results | Next → Later | EP-5, EP-6, EP-7, EP-8, F-1.7 |
| EP-10 | Fair Transfer & Returns | Next → Later | EP-7, EP-3, EP-6 |
| EP-11 | Realtime Monitoring | Next → Later | EP-5, EP-6, EP-8 |

### 0.5 Roadmap by phase (feature-level)
- **MVP:** F-1.1, F-1.2, F-1.5, F-1.6 · F-2.1, F-2.2, F-2.3, F-2.4, F-2.5 · F-3.1, F-3.2, F-3.3, F-3.4 · F-4.1, F-4.2 · F-5.1, F-5.2, F-5.3, F-5.4, F-5.5, F-5.6 · F-6.1, F-6.2, F-6.3, F-6.4, F-6.5 · F-7.1, F-7.2, F-7.3, F-7.4 · F-8.1, F-8.2, F-8.3, F-8.4
- **Next:** F-1.3, F-1.7, F-1.8 · F-2.6 · F-3.5, F-3.6, F-3.7, F-3.8 · F-4.3, F-4.4, F-4.5 · F-6.6 · F-7.5 · F-8.5 · F-9.1, F-9.2, F-9.3, F-9.4 · F-10.1, F-10.2 · F-11.1
- **Later:** F-1.4, F-1.9 · F-2.7 · F-7.6 · F-8.6 · F-9.5, F-9.6 · F-10.3, F-10.4 · F-11.2, F-11.3

---

## EP-1 — Organizer Accounts & Identity
**Goal:** Let an organizer create an account and sign in, define roles and permissions, and control who can do what on each event — so every action has an identity and an authorization behind it.
**Depends on:** nothing — this is the foundation every other epic stands on.

#### F-1.1 — Register an organizer account
`MVP · serves PER-O1, PER-O2 · depends on —`
A person creates an organizer account with an email, a password, and a display name.
- *Story:* As an organizer, I want to create an account so that I can build and own events.
- **Acceptance criteria**
  - Given a unique email and a password that meets the rules, when I register, then my account is created and I am signed in.
  - Given an email already in use, when I register, then I am told the email is taken and no account is created.
  - Given a password that fails the rules, when I register, then I am told why and no account is created.

#### F-1.2 — Sign in and sign out
`MVP · serves PER-O1, PER-O2 · depends on F-1.1`
An organizer signs in to get a session and signs out to end it.
- *Story:* As an organizer, I want to sign in so that I can return to my events.
- **Acceptance criteria**
  - Given valid credentials, when I sign in, then I have an active session and reach my organizer area.
  - Given invalid credentials, when I sign in, then I am refused without revealing which field was wrong.
  - Given an active session, when I sign out, then the session is ended and protected areas are no longer accessible.

#### F-1.3 — Manage organizer profile
`Next · serves PER-O1, PER-O2 · depends on F-1.1`
An organizer edits their display name, contact email, and optional avatar.
- **Acceptance criteria**
  - When I update my profile, then the new details are saved and shown.
  - When I upload an avatar, then it is stored and displayed; an invalid file is rejected with a clear message.
- *Note:* avatar is an image asset (stored as described in `technical-design.md` §5).

#### F-1.4 — Optional attendee account
`Later · serves PER-A1, PER-A2 · depends on F-1.1`
An attendee may optionally create an account to keep their tickets together; buying never requires it (guest checkout stays — see F-5.2).
- **Acceptance criteria**
  - When an attendee creates an account, then tickets bought with their email can be linked and viewed in one place (see F-7.6).
  - Given no account, when an attendee buys, then the purchase still completes as a guest.

#### F-1.5 — Define roles and permissions
`MVP · serves PER-O1, PER-O2 · depends on F-1.1`
Establish a set of roles with distinct permission sets that govern what a user can do on an event.
- *Story:* As an organizer, I want to define roles so that I can control who can manage my events and what they can do.
- **Acceptance criteria**
  - The system defines at least two roles: **Owner** (full control over the event — create, edit, publish, cancel, manage tickets, manage staff, check-in, view results) and **Staff** (limited to check-in operations and viewing attendee lists for assigned events).
  - Permissions are grouped by capability area: Event Management, Ticketing, Check-in, Reporting, and Staff Management.
  - A role's permissions are non-overlapping with system-level account capabilities (e.g., creating events is an Owner action, not a global permission).

#### F-1.6 — Assign roles to users per event
`MVP · serves PER-O1, PER-O2 · depends on F-1.5`
An event owner can assign roles to other users for that specific event.
- *Story:* As an organizer, I want to assign staff to my event so that they can help with check-in without having full control.
- **Acceptance criteria**
  - When I assign a user to a role for my event, then that user gains the permissions of that role for that event only.
  - A user can hold different roles on different events (e.g., Staff on one event, Owner on another).
  - An event must have exactly one Owner; assigning a new Owner transfers ownership and demotes the previous Owner to Staff.
  - I cannot assign a role to a user who already holds that same role on the event (duplicate assignment is rejected).

#### F-1.7 — Role-based access control for event operations
`Next · serves PER-O1, PER-O2 · depends on F-1.6`
Every event operation checks the caller's role before executing.
- *Story:* As an organizer, I want my event operations protected so that only authorized users can perform them.
- **Acceptance criteria**
  - When a user attempts an operation (edit event, publish, cancel, manage tickets, check-in, view results), then the system verifies they hold a role with the required permission for that event.
  - A user without the required role is refused with a clear "insufficient permissions" message; the operation does not execute.
  - The check-in scan (F-8.1) requires the Check-in permission; a Staff user with only Check-in permission cannot edit the event or manage tickets.
  - Public operations (viewing published event pages, purchasing tickets — EP-4, EP-5) are not affected by RBAC and remain accessible to all visitors.

#### F-1.8 — Invite staff to an event
`Next · serves PER-O1, PER-O2 · depends on F-1.6, F-7.2`
An organizer invites a person by email to become Staff on an event.
- *Story:* As an organizer, I want to invite my team members by email so that they can help run the event.
- **Acceptance criteria**
  - When I invite a person by email with the Staff role, then an invitation email is sent; upon acceptance, the person is assigned the Staff role for that event.
  - If the invitee does not have an account, they are prompted to register (F-1.1) before accepting.
  - An invitation can be revoked before acceptance; a revoked invitation cannot be accepted.
  - An invitation expires after a configurable window (default 7 days); an expired invitation cannot be accepted.

#### F-1.9 — Permission audit log
`Later · serves PER-O1, PER-O2 · depends on F-1.7`
A log of role assignments and permission changes for accountability.
- *Story:* As an organizer, I want to see who changed permissions and when so that I can audit access to my events.
- **Acceptance criteria**
  - When a role is assigned, revoked, or transferred, then an audit entry is created recording the actor, the target user, the event, the old role (if any), the new role (if any), and the timestamp.
  - I can view the audit log for my events, filtered by date range and action type.
  - Audit entries are immutable once created.

---

## EP-2 — Event Creation & Management
**Goal:** Let organizers create events and move them through their lifecycle.
**Depends on:** EP-1 — only a signed-in organizer can create an event, and every event has an owner.
**Builds on previous:** uses the identity from EP-1 to attach ownership and authorize edits.

#### F-2.1 — Create a draft event
`MVP · serves PER-O1, PER-O2 · depends on F-1.2`
An organizer creates an event with its core details; it starts as a private draft.
- *Story:* As an organizer, I want to create an event so that I can begin setting it up before selling.
- **Acceptance criteria**
  - Given I am signed in, when I provide a title, start/end date-time with a time zone, and a location (a physical address **or** marked online), then a new event is created in **Draft**, owned by me with the Owner role (F-1.5).
  - A draft is visible only to its owner.
  - Missing required fields block creation with a clear message.

#### F-2.2 — Add an event cover image
`MVP · serves PER-O1, PER-O2 · depends on F-2.1`
An organizer uploads a cover image shown on the public page.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I upload a supported image within the size limit, then it is stored and shown as the event cover.
  - A user without the Owner role is refused with an "insufficient permissions" message.
  - An unsupported or oversized file is rejected with a clear message.
- *Note:* the image is stored as an object (see `technical-design.md` §5); the event keeps a reference, not the bytes.

#### F-2.3 — Edit event details
`MVP · serves PER-O1, PER-O2 · depends on F-2.1`
An organizer edits an event, with safeguards once it is live.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), while **Draft**, I can edit any detail freely.
  - Given the Owner role, while **Published**, I can edit descriptive fields, but a change that would harm sold tickets (for example, reducing a ticket type's capacity below the number already sold) is blocked with an explanation.
  - A user without the Owner role is refused with an "insufficient permissions" message.

#### F-2.4 — Publish an event
`MVP · serves PER-O1, PER-O2 · depends on F-2.1, F-3.1`
An organizer makes an event public and sellable.
- *Story:* As an organizer, I want to publish so that people can find and buy tickets.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5) and the event has the required details and **at least one ticket type** (F-3.1), when I publish, then it moves **Draft → Published** and receives a stable public link (used by EP-4).
  - A user without the Owner role is refused with an "insufficient permissions" message.
  - Publishing is blocked, with reasons, if requirements are unmet.

#### F-2.5 — Close or cancel an event
`MVP · serves PER-O1, PER-O2 · depends on F-2.4`
An organizer stops sales (Close) or cancels the event (Cancel).
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I **Close** an event, then no new purchases are allowed but issued tickets remain valid for entry.
  - Given the Owner role, when I **Cancel** an event, then sales stop and it is marked cancelled; once payments exist, cancellation triggers refunds (F-6.6).
  - A user without the Owner role is refused with an "insufficient permissions" message.
  - Attendees of a cancelled event can be notified (light messaging, F-9.5, when available).

#### F-2.6 — Duplicate an event
`Next · serves PER-O1, PER-O2 · depends on F-2.1`
An organizer clones a past event's setup to reuse it.
- **Acceptance criteria**
  - Given I hold the Owner role for the source event (F-1.5), when I duplicate an event, then a new **Draft** is created copying details and ticket-type definitions (not sales, attendees, or dates), and I become the Owner of the new event.
  - A user without the Owner role is refused with an "insufficient permissions" message.

#### F-2.7 — Multiple occurrences / sessions
`Later · serves PER-O2 · depends on F-2.1`
An organizer offers an event that runs on several dates.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I add occurrences, then each occurrence has its own date and its own inventory, while sharing the event's description and page.
  - A user without the Owner role is refused with an "insufficient permissions" message.
- *Note:* optional; kept out of the core to protect simplicity (`product-requirements.md` QG-1).

---

## EP-3 — Ticketing & Transparent Pricing
**Goal:** Define what can be sold, at what final price, and how much exists — and guarantee the same seat is never sold twice.
**Depends on:** EP-2 — ticket types belong to an event.
**Builds on previous:** attaches sellable inventory and pricing to the events created in EP-2; what is defined here is what EP-4 displays and EP-5 sells.

#### F-3.1 — Define a ticket type
`MVP · serves PER-O1, PER-O2 · depends on F-2.1`
An organizer creates a ticket type with a name, a price, and a quantity.
- *Story:* As an organizer, I want to define a ticket so that there is something to sell.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I add a ticket type with a name, a price (in the configured currency), and a capacity, then it is attached to the event.
  - A user without the Owner role is refused with an "insufficient permissions" message.
  - A price of zero makes it a **free** ticket type (F-3.2).
  - An event must have at least one ticket type before it can be published (F-2.4).

#### F-3.2 — Free tickets
`MVP · serves PER-O1, PER-O2 · depends on F-3.1`
Support events and tickets that cost nothing.
- **Acceptance criteria**
  - Given a free ticket type, when an attendee checks out, then no payment step is required and the order is confirmed immediately (F-6.2).
  - A free event is free to create and to attend (`product-requirements.md` DEC-1).

#### F-3.3 — Transparent, all-inclusive pricing
`MVP · serves PER-A1, PER-A2 · depends on F-3.1`
The price an attendee sees is the price they pay — no fees appear later.
- *Story:* As an attendee, I want the price shown to be the price charged so that there are no surprises.
- **Acceptance criteria**
  - The price shown on the public page (EP-4) and in the checkout summary (F-5.4) equals the amount charged.
  - No platform fee is added at any step (`product-requirements.md` DEC-1).

#### F-3.4 — Inventory and no-oversell guarantee
`MVP · serves PER-O1, PER-A1 · depends on F-3.1`
Track availability and never sell beyond capacity, even under simultaneous buyers.
- **Acceptance criteria**
  - Available = capacity − reserved − sold; when it reaches zero, the ticket type shows **sold out** and cannot be added to an order.
  - Given two attendees racing for the last ticket, when both try to buy, then exactly one succeeds and the other is told it sold out.
- *Note:* the consistency mechanism is in `technical-design.md` §6.

#### F-3.5 — Multiple ticket types per event
`Next · serves PER-O1, PER-O2 · depends on F-3.1`
Offer several tiers (for example General, VIP, Early-bird), each with its own price and capacity.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I add more than one ticket type, then each is sold and tracked independently and all appear on the event page.
  - A user without the Owner role is refused with an "insufficient permissions" message.

#### F-3.6 — Per-order purchase limit
`Next · serves PER-O1 · depends on F-3.1`
Cap how many tickets one order may buy, to keep access fair.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I set a limit, then an order exceeding it is blocked with a clear message.
  - A user without the Owner role is refused with an "insufficient permissions" message.

#### F-3.7 — Discount codes
`Next · serves PER-O1, PER-O2 · depends on F-3.1, F-3.3`
An organizer issues codes that reduce the price transparently at checkout.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I create a code (percentage or fixed amount, with an optional validity window and usage cap), then a valid code applied at checkout lowers the final total, and the discounted total is the amount charged.
  - A user without the Owner role is refused with an "insufficient permissions" message.
  - An expired, exhausted, or unknown code is rejected without changing the price.

#### F-3.8 — Scheduled on-sale window
`Next · serves PER-O1, PER-O2 · depends on F-3.1`
A ticket type can be on sale only between set times.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I set a sales window, then before the start time the ticket type is not purchasable; between start and end it is; after the end it stops selling — all reflected on the event page.
  - A user without the Owner role is refused with an "insufficient permissions" message.

---

## EP-4 — Event Discovery & Access
**Goal:** Let attendees reach an event — first by a shared link, later by browsing and search.
**Depends on:** EP-2 (a published event) and EP-3 (ticket types and prices to show).
**Builds on previous:** turns a published event with priced inventory into something an attendee can open and act on.

#### F-4.1 — Shareable public event page
`MVP · serves PER-A1, PER-A2 · depends on F-2.4, F-3.3`
Every published event has a public page at a stable link.
- *Story:* As an organizer, I want a link I can share so that people can buy without any account or search.
- **Acceptance criteria**
  - Given a published event, when anyone opens its link, then they see the title, description, date/time, location, cover image, and each ticket type with its final price, plus a way to start buying (EP-5).
  - The page works for visitors with no account.
  - A draft, closed, or cancelled event shows an appropriate state instead of a buy option.

#### F-4.2 — Mobile-friendly event page
`MVP · serves PER-A1, PER-A2 · depends on F-4.1`
The public page works well on a phone.
- **Acceptance criteria**
  - On a typical phone screen, the page is readable and the buy flow is usable without horizontal scrolling or zooming (`product-requirements.md` QG-4).

#### F-4.3 — Rich link previews
`Next · serves PER-O1, PER-O2 · depends on F-4.1`
Shared links show a nice preview on social and chat.
- **Acceptance criteria**
  - When a link is shared, then the preview shows the event title, cover image, and date.

#### F-4.4 — Public event listing
`Next · serves PER-A1 · depends on F-4.1`
A simple page that lists public events for browsing.
- **Acceptance criteria**
  - When I open the listing, then I see currently public, upcoming events, each linking to its page.

#### F-4.5 — Search and filter
`Next · serves PER-A1 · depends on F-4.4`
Find events by keyword, date, or category/location.
- **Acceptance criteria**
  - When I search or filter, then the listing narrows to matching public events.

---

## EP-5 — Purchase & Checkout
**Goal:** Let an attendee choose tickets and place an order, holding inventory while they finish.
**Depends on:** EP-4 (they found the event) and EP-3 (ticket types, availability, limits).
**Builds on previous:** consumes the inventory model from EP-3 and the public page from EP-4 to create an order that EP-6 will charge.

#### F-5.1 — Select tickets and start checkout
`MVP · serves PER-A1, PER-A2 · depends on F-4.1, F-3.4`
From the event page, an attendee picks ticket types and quantities.
- *Story:* As an attendee, I want to choose my tickets so that I can buy them.
- **Acceptance criteria**
  - When I choose quantities within availability and any per-order limit (F-3.6), then I can proceed to checkout.
  - A choice exceeding availability or a limit is prevented with a clear message.

#### F-5.2 — Guest checkout
`MVP · serves PER-A1, PER-A2 · depends on F-5.1`
An attendee buys with just a name and email — no account.
- **Acceptance criteria**
  - When I provide a name and a valid email, then I can complete the purchase without creating an account.

#### F-5.3 — Create order and hold inventory
`MVP · serves PER-A1, PER-A2 · depends on F-5.1, F-3.4`
Placing an order reserves the chosen tickets for a limited time.
- **Acceptance criteria**
  - When I place an order, then a **Pending** order is created and the chosen quantity is reserved so others cannot take it.
  - The reservation does not reduce another buyer's ability to see correct availability.

#### F-5.4 — Final price summary
`MVP · serves PER-A1, PER-A2 · depends on F-5.3, F-3.3`
Before paying, the attendee sees exactly what they will pay.
- **Acceptance criteria**
  - When I reach the summary, then I see each line item and a final total that equals the amount to be charged (including any discount from F-3.7 when present).

#### F-5.5 — Hold expiry and release
`MVP · serves PER-A1 · depends on F-5.3`
Unfinished orders release their inventory.
- **Acceptance criteria**
  - Given a Pending order whose hold window passes without payment, when the window expires, then the order is marked expired and the reserved tickets return to availability.

#### F-5.6 — View order status
`MVP · serves PER-A1, PER-A2 · depends on F-5.3`
An attendee can check an order via a link/reference, without an account.
- **Acceptance criteria**
  - When I open my order reference, then I see its status (pending, confirmed, expired, cancelled) and, once confirmed, my tickets (EP-7).

---

## EP-6 — Payment
**Goal:** Charge paid orders through a trusted external provider, confirm reliably, and never hold funds or take a cut.
**Depends on:** EP-5 — there must be an order to pay for.
**Builds on previous:** takes the Pending order from EP-5 to confirmation, which then triggers ticket issuance in EP-7.

#### F-6.1 — Pay for an order via the provider
`MVP · serves PER-A1, PER-A2 · depends on F-5.3`
A paid order is settled through the external payment provider.
- *Story:* As an attendee, I want to pay securely so that I receive my tickets.
- **Acceptance criteria**
  - Given a Pending order with a non-zero total, when I choose to pay, then I am taken to the provider to complete payment.
  - EventHub never collects or stores card details (`product-requirements.md` QG-6).

#### F-6.2 — Free-order auto-confirm
`MVP · serves PER-A1, PER-A2 · depends on F-3.2, F-5.3`
Zero-total orders skip payment.
- **Acceptance criteria**
  - Given an order whose total is zero, when I place it, then it is confirmed immediately without a payment step and proceeds to ticket issuance (EP-7).

#### F-6.3 — Confirm payment reliably
`MVP · serves PER-A1, PER-A2 · depends on F-6.1`
Confirmation is driven by the provider and is safe against duplicates.
- **Acceptance criteria**
  - When the provider reports a successful payment, then the order becomes **Confirmed** and tickets are issued exactly once, even if the notification is delivered more than once.

#### F-6.4 — Handle payment failure or abandonment
`MVP · serves PER-A1 · depends on F-6.1, F-5.5`
A failed or abandoned payment must not leave a confirmed order or stuck inventory.
- **Acceptance criteria**
  - When payment fails or is abandoned, then the order is not confirmed, the hold expires normally (F-5.5), and I can try again.

#### F-6.5 — No platform fee; funds go to the organizer
`MVP · serves PER-O1, PER-O2 · depends on F-6.1`
EventHub takes no cut and holds no money.
- **Acceptance criteria**
  - The amount charged equals the displayed total with no EventHub fee added; funds settle to the organizer through the provider, not through EventHub (`product-requirements.md` DEC-1).

#### F-6.6 — Refund on cancellation
`Next · serves PER-O1, PER-A1 · depends on F-6.3, F-2.5`
Cancelling an event refunds its paid orders. Consumes the `EVT-EventCancelled` integration event raised by F-2.5.
- **Acceptance criteria**
  - When an organizer cancels an event (F-2.5), then each paid order is refunded through the provider and marked refunded, and the affected tickets are invalidated.
  - The refund is triggered by the `EVT-EventCancelled` integration event, ensuring the cancellation intent (recorded in F-2.5) is acted upon even if refunds are processed asynchronously.

---

## EP-7 — Ticket Delivery & Attendee Access
**Goal:** Turn a confirmed order into valid tickets and get them to the attendee.
**Depends on:** EP-6 — tickets are issued only for a confirmed order.
**Builds on previous:** consumes confirmation from EP-6 to mint the tickets that EP-8 will scan.

#### F-7.1 — Issue tickets on confirmation
`MVP · serves PER-A1, PER-A2 · depends on F-6.3, F-6.2`
Each confirmed unit becomes a ticket with a unique scannable code.
- **Acceptance criteria**
  - When an order is confirmed, then one ticket is created per purchased unit, each with a unique code (QR) tied to the event and the buyer.

#### F-7.2 — Deliver tickets by email
`MVP · serves PER-A1, PER-A2 · depends on F-7.1`
Tickets are emailed to the buyer.
- **Acceptance criteria**
  - When tickets are issued, then the buyer receives an email containing the tickets (QR and event details).
  - Delivery happens reliably even if it is processed shortly after confirmation (handled asynchronously — see `technical-design.md` §5).

#### F-7.3 — Access tickets via link
`MVP · serves PER-A1, PER-A2 · depends on F-7.1`
Tickets are openable from a link/reference without an account.
- **Acceptance criteria**
  - When I open my order/ticket link, then I can view my QR tickets without signing in.

#### F-7.4 — Mobile ticket display
`MVP · serves PER-A1, PER-A2 · depends on F-7.3`
The QR shows clearly on a phone for scanning at the door.
- **Acceptance criteria**
  - On a phone, the QR is large and clear enough to be scanned, and each ticket is individually viewable (supporting PER-A2 sharing tickets with friends).

#### F-7.5 — Resend / recover tickets
`Next · serves PER-A1, PER-A2 · depends on F-7.2`
An attendee can have tickets re-sent.
- **Acceptance criteria**
  - When I request a resend for my email/order, then the tickets are delivered again.

#### F-7.6 — Attendee ticket wallet
`Later · serves PER-A1, PER-A2 · depends on F-1.4, F-7.1`
A signed-in attendee sees all their tickets in one place.
- **Acceptance criteria**
  - Given an attendee account (F-1.4), when I sign in, then I see tickets across all events bought with my email.

---

## EP-8 — Event-Day Check-in
**Goal:** Validate tickets at the door and prevent double entry.
**Depends on:** EP-7 (issued tickets to scan) and F-1.7 (role-based access control for check-in authorization).
**Builds on previous:** scans the codes minted in EP-7 and produces the attendance data that EP-9 and EP-11 report on.

#### F-8.1 — Scan and validate a ticket
`MVP · serves PER-O1, PER-O2 · depends on F-7.1, F-2.4`
Staff scan a QR to admit an attendee.
- *Story:* As an organizer at the door, I want to scan tickets so that only valid holders enter.
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Check-in permission for the event (F-1.5), when I scan a valid, unused ticket for this event, then it is accepted and marked **checked in**.
  - A user without Check-in permission is refused with an "insufficient permissions" message.
  - A code for a different event, a cancelled order, or an unknown code is rejected with a clear reason.

#### F-8.2 — Prevent double scan
`MVP · serves PER-O1, PER-O2 · depends on F-8.1`
A ticket admits exactly once.
- **Acceptance criteria**
  - When an already-checked-in ticket is scanned again, then it is rejected with a clear "already checked in" message (including the first check-in time).

#### F-8.3 — Manual lookup and check-in
`MVP · serves PER-O1, PER-O2 · depends on F-8.1`
Check in by searching when scanning is not possible.
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Check-in permission for the event (F-1.5), when I search by code or buyer email, then I can find the matching ticket and check it in manually, with the same double-entry protection (F-8.2).
  - A user without Check-in permission is refused with an "insufficient permissions" message.

#### F-8.4 — Door counts
`MVP · serves PER-O1, PER-O2 · depends on F-8.1`
A simple running tally at the door.
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Check-in permission for the event (F-1.5), during check-in, I can see how many tickets are checked in versus the total issued.
  - A user without Check-in permission cannot view door counts.

#### F-8.5 — Multiple staff / devices
`Next · serves PER-O2 · depends on F-8.1, F-1.7`
Several people check in at once without conflicts.
- **Acceptance criteria**
  - Given two staff scanning at the same time, when the same ticket is presented twice, then only the first is accepted (consistency holds across devices).

#### F-8.6 — Offline-tolerant scanning
`Later · serves PER-O2 · depends on F-8.1`
Keep scanning through brief connectivity gaps.
- **Acceptance criteria**
  - When connectivity drops briefly, then scanning continues and results reconcile once connection returns, still preventing double entry.
- *Note:* optional; included only if door conditions demand it (kept out of the core for simplicity).

---

## EP-9 — Audience & Results
**Goal:** Give organizers the audience data they own and simple results, then the ability to message attendees.
**Depends on:** EP-5–EP-8 — there must be orders, attendees, tickets, and check-ins to work with. Also F-1.7 (role-based access control for reporting authorization).
**Builds on previous:** reads the records produced across the purchase, payment, delivery, and check-in epics.

#### F-9.1 — Attendee list per event
`Next · serves PER-O1, PER-O2 · depends on F-5.2, F-7.1, F-8.1`
The organizer sees everyone who bought or attended.
- *Story:* As an organizer, I want my attendee list so that I own my audience relationship (`product-requirements.md` G-5).
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Reporting permission for the event (F-1.5), I can see each attendee's name, email, ticket type, order, and check-in status.
  - A user without Reporting permission is refused with an "insufficient permissions" message.

#### F-9.2 — Export attendees
`Next · serves PER-O1, PER-O2 · depends on F-9.1`
Take the audience data elsewhere.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I export, then I get a CSV of the attendee list for my event.
  - A user without the Owner role (including Staff) is refused with an "insufficient permissions" message.

#### F-9.3 — Sales and attendance results
`Next · serves PER-O1, PER-O2 · depends on F-3.1, F-6.3, F-8.1`
A simple results view per event.
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Reporting permission for the event (F-1.5), I can see tickets sold by type, total revenue (which equals gross, since there is no platform fee), the check-in rate, and the number of no-shows — the figures behind the North Star (`product-requirements.md` §3.2).
  - A user without Reporting permission is refused with an "insufficient permissions" message.

#### F-9.4 — Organizer events overview
`Next · serves PER-O1, PER-O2 · depends on F-2.1, F-9.3`
A home view of all the organizer's events.
- **Acceptance criteria**
  - Given I am signed in, I see a list of events where I hold the Owner role (F-1.5), with quick stats (sold, revenue, date, status), and can open any one's results.
  - Events where I hold only the Staff role appear in a separate section with check-in stats only.

#### F-9.5 — Light messaging to attendees
`Later · serves PER-O1, PER-O2 · depends on F-9.1, F-7.2`
Email an event's attendees.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I send a message to an event's attendees (for example a reminder, update, or cancellation notice), then it is delivered to their emails (processed asynchronously — see `technical-design.md` §5).
  - A user without the Owner role (including Staff) is refused with an "insufficient permissions" message.

#### F-9.6 — Automatic event reminder
`Later · serves PER-O1, PER-A1 · depends on F-9.5`
An optional reminder before the event.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when I enable a reminder, then attendees receive an email a set time before the event.
  - A user without the Owner role is refused with an "insufficient permissions" message.

---

## EP-10 — Fair Transfer & Returns (Anti-scalping)
**Goal:** Let attendees pass tickets on fairly, without enabling scalping.
**Depends on:** EP-7 (issued tickets), EP-3 (inventory, for returns), EP-6 (refunds, for returns).
**Builds on previous:** acts on the tickets from EP-7 and, for returns, the inventory from EP-3 and refunds from EP-6. Realizes `product-requirements.md` DEC-2.

#### F-10.1 — Transfer a ticket at face value
`Next · serves PER-A1, PER-A2 · depends on F-7.1`
A holder gives a ticket to someone else, with no markup.
- *Story:* As an attendee who can no longer attend, I want to pass my ticket to a friend so that it is not wasted.
- **Acceptance criteria**
  - When I transfer a ticket to a recipient's email, then a fresh ticket (new code) is issued to them and my original is invalidated.
  - No money is collected by EventHub for a transfer, and there is no way to set a price — transfers are face value only (`product-requirements.md` DEC-2).

#### F-10.2 — Transfer safeguards
`Next · serves PER-A1, PER-O1 · depends on F-10.1, F-8.1`
Transfers cannot be abused for entry.
- **Acceptance criteria**
  - A checked-in ticket cannot be transferred.
  - After a transfer, the old code no longer admits anyone; only the new holder's code is valid.

#### F-10.3 — Return a ticket to the pool at face value
`Later · serves PER-A1, PER-O1 · depends on F-10.1, F-3.4, F-6.6`
For a sold-out event, a holder can hand a ticket back to be re-sold.
- **Acceptance criteria**
  - Given a sold-out event, when a holder returns a ticket, then it re-enters availability to be sold again at face value, and the returner is refunded through the provider.
  - Given I hold the Owner role for the event (F-1.5), I can approve or process the return; a user without the Owner role cannot initiate returns on behalf of the event.

#### F-10.4 — Return eligibility and limits
`Later · serves PER-O1 · depends on F-10.3`
Clear, fair rules for returns.
- **Acceptance criteria**
  - Returns are allowed only while the event is sold out and before a set cutoff; outside those conditions, the option is unavailable with an explanation.

---

## EP-11 — Realtime Monitoring (enhancement)
**Goal:** Give organizers live visibility into sales and check-in, using the realtime stack.
**Depends on:** EP-5/EP-6 (sales) and EP-8 (check-in).
**Builds on previous:** pushes live updates derived from the sales and check-in data already produced; it adds no new source data, only immediacy.

#### F-11.1 — Live sales and inventory
`Next · serves PER-O1, PER-O2 · depends on F-5.3, F-3.4`
The organizer's event view updates as tickets sell.
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Reporting permission for the event (F-1.5), when tickets are bought, then my open event view updates the sold count and remaining availability without a manual refresh (delivered in real time — see `technical-design.md` §5).
  - A user without Reporting permission does not receive live sales updates.

#### F-11.2 — Live check-in progress
`Later · serves PER-O1, PER-O2 · depends on F-8.4, F-8.5`
The door count updates live across staff devices.
- **Acceptance criteria**
  - Given I hold the Owner or Staff role with Check-in permission for the event (F-1.5), during check-in, the checked-in count updates in real time and stays consistent across all staff devices.
  - A user without Check-in permission does not receive live check-in updates.

#### F-11.3 — Sold-out / low-stock nudges
`Later · serves PER-O1, PER-O2 · depends on F-3.4, F-11.1`
A live heads-up as inventory runs low.
- **Acceptance criteria**
  - Given I hold the Owner role for the event (F-1.5), when a ticket type is nearly or fully sold, then I receive a real-time notification.
  - A user without the Owner role (including Staff) does not receive sold-out nudges.

---

*This document is a living draft for a personal project. It is the feature reference for EventHub; product intent is in `product-requirements.md` and technical design is in `technical-design.md`. Phases and dependencies describe the intended build order and may be revised as the project evolves.*
