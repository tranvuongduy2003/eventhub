---
document_type: PRD
document_subtype: strategic_product_requirements
product_name: EventHub
one_liner: A simple, fair event management and ticketing platform for small events, connecting organizers and attendees.
version: "3.0"
status: draft
last_updated: "2026-06-14"
owner: Builder (solo / pet project)
project_type: pet_project
intended_scale: small_events_modest_concurrency
primary_audience: [builder, product, design, engineering, ai_agents]
language: en
scope_level: strategy_and_product_intent
deliberately_excluded:
  - detailed_feature_specifications             # see companion document: feature-specification.md
  - technical_architecture_and_implementation   # see companion document: technical-design.md
  - market_and_competitive_analysis             # out of scope by decision
companion_documents:
  features: feature-specification.md
  domain: domain-model-specification.md
  technical: technical-design.md
---

# EventHub — Product Requirements Document (PRD)
**A simple, fair event management and ticketing platform for small events.**

---

## 0. About This Document (read first)

### 0.1. Purpose
This PRD defines **why EventHub exists, who it serves, what problem it solves, how it is scoped, and how its success is judged.** It captures product intent and direction for a **personal (pet) project**. It is the top-level reference that the companion documents expand upon.

### 0.2. What this document does NOT contain
This document **intentionally excludes**:
- **Specific feature specifications** (capabilities, user stories, acceptance criteria, screen-by-screen behavior) → see `feature-specification.md`.
- **Specific technical details** (architecture, data models, integrations, infrastructure, security implementation) → see `technical-design.md`.
- **Market sizing and competitive analysis** — excluded by decision; not relevant to the goals of this project.

Where this PRD references a capability or a quality expectation, it does so at the level of **intent and outcome**, not implementation.

### 0.3. Reading guide for AI agents and automated readers
- This document scopes a **small personal project**, not a large commercial venture. Interpret all goals, metrics, and quality expectations at the scale of **small events with modest concurrent demand**, built and maintained by a solo (or very small) team.
- A **stable, prefixed identifier scheme** makes items referenceable and traceable. Prefixes: `G` = Goal, `PER` = Persona, `QG` = Quality Guardrail, `KPI` = Metric, `RSK` = Risk, `DEC` = Decision, `ASM` = Assumption, `DEP` = Dependency.
- Statements are explicit and self-contained. Acronyms are expanded on first use and defined in the Glossary (Section 12).
- When a topic belongs to a companion document, this is stated with a pointer such as `→ see feature-specification.md`.
- The items in **Section 11.1 (Key Decisions)** are **resolved and intentional**. They are revisable, but should be treated as the current chosen answer, not as open questions.

---

## 1. Executive Summary

EventHub is a **small two-sided platform** that connects two groups of users:

1. **Organizers** — individuals or small groups who create an event, sell tickets, collect payment, look after their attendees, and see simple results.
2. **Attendees** — people who discover an event, buy a ticket, and attend without friction.

This is a **pet project**. Its purpose is to build a clean, fair, and genuinely working end-to-end product for **small events** — not to operate at large scale or to compete commercially. The guiding idea is **"simple, fair, and it just works"**: transparent all-inclusive pricing, a smooth mobile purchase, valid tickets, and a straightforward way for an organizer to run an event from creation through to entry and basic results.

This document defines the problem, the users, the scope boundaries, the (minimal) business model, the goals, the success measures, the risks, and the key decisions. **Feature definitions and technical design are out of scope here** and live in `feature-specification.md` and `technical-design.md`.

---

## 2. Problem & Motivation

### 2.1. The organizer's problem
A small organizer running a workshop, class, meetup, or modest show usually juggles several disconnected tools: a page to advertise the event, a separate way to take payment, a spreadsheet to track who is coming, and a manual process at the door. This is tedious, error-prone, and unprofessional. On existing platforms, they also often face fees they cannot clearly predict and have little ownership of their own attendee list.

### 2.2. The attendee's problem
Attendees often meet fees that only appear at the final checkout step, purchase flows that are awkward on a phone, and no trustworthy way to hand a ticket to someone else when plans change. The price they expected is rarely the price they pay.

### 2.3. Why build EventHub
The goal is to provide a **single, simple, fair tool** that lets a small organizer run an event end-to-end, and lets an attendee buy and attend with no surprises. As a pet project, it is also a way to design and build a complete, well-crafted, two-sided product — start to finish.

---

## 3. Vision & Goals

### 3.1. Vision
> A simple, fair, and well-built ticketing tool that lets a small organizer run an event end-to-end — and lets attendees buy and attend without friction or surprises.

### 3.2. North Star
**The number of events successfully run end-to-end on the platform** — meaning tickets were sold and attendees were actually checked in at the door. This captures real value delivered to both sides, not just sign-ups.

### 3.3. Goals

| ID | Goal | Intent |
|---|---|---|
| **G-1** | Ship a working end-to-end product | Make it possible to take one small event all the way: create → sell tickets → attend (check-in) → see basic results. |
| **G-2** | Stay simple and maintainable | Keep scope small enough for a solo builder to finish and maintain; prefer fewer things done well; resist scope creep. |
| **G-3** | Be fair and transparent | All-inclusive pricing shown upfront, no hidden fees, and no enabling of ticket scalping. |
| **G-4** | Make buying smooth on mobile | The attendee can find an event and buy a ticket quickly on a phone. |
| **G-5** | Give the organizer clarity and ownership | Simple, useful results, and ownership of their own attendee list. |

### 3.4. Experience intent (outcomes, not features)
- An organizer can set up and publish a sellable event quickly, without specialist help.
- An attendee can complete a purchase on a phone in seconds, with the **final price shown from the start**.
- The product works correctly for the modest demand of a small event and never sells the same seat twice.

---

## 4. Target Users & Personas

### 4.1. Organizers (supply side)

| ID | Persona | Core need |
|---|---|---|
| **PER-O1** | Individual organizer (workshop, class, meetup, community talk) | Fast, simple setup; free events should cost nothing to run. |
| **PER-O2** | Small group / club / small business (modest paid events) | A little more structure — multiple ticket types, simple discounts, and basic reporting. |

### 4.2. Attendees (demand side)

| ID | Persona | Core need |
|---|---|---|
| **PER-A1** | General attendee | A fast mobile purchase, a clear price, and a valid ticket. |
| **PER-A2** | Group buyer (buying for friends) | Buying a few tickets at once and sharing them easily. |

---

## 5. Value Journeys (high-level)

These describe the **lifecycle stages** each side moves through. The specific capabilities behind each stage are defined in `feature-specification.md`.

### 5.1. Organizer lifecycle
**Set up the event → Configure tickets (including free) → Share / publish → Sell and watch sales → Run entry (check-in) → Receive payment → See basic results.**

### 5.2. Attendee lifecycle
**Find the event → See the upfront, all-inclusive price → Buy → Receive the ticket → Attend → (if plans change, transfer the ticket fairly).**

---

## 6. Scope & Boundaries

### 6.1. Capability pillars (what the product is fundamentally about)
At the strategic level, EventHub covers these value areas. The detailed capabilities within each are defined in `feature-specification.md`.
1. **Event creation and management.**
2. **Ticketing and transparent (all-inclusive) pricing.**
3. **Payment and payout** (handled through a trusted provider; see Decision DEC-1).
4. **Attendee access and discovery** (at minimum, a shareable event link; a simple public listing later).
5. **Purchase and ticket delivery.**
6. **Event-day check-in.**
7. **Basic audience care and results** (a simple attendee list and simple sales results).
8. **Basic trust and fairness** (valid tickets, no hidden fees, fair face-value transfer).

### 6.2. Non-goals (explicitly out of scope)
To protect simplicity, the following are deliberately **not** in scope:
- Large-scale / high-concurrency "on-sale" handling.
- Enterprise, venue, or promoter features.
- Multiple currencies or multiple countries.
- Blockchain / NFT ticketing.
- Ticketed live-streaming or hybrid-event delivery.
- An open, paid secondary-resale marketplace (only fair, face-value transfer is supported — see DEC-2).
- An in-product social network.
- A platform fee at launch (monetization is deferred — see DEC-1).

### 6.3. Document boundaries (where detail lives)
- **What the product does, feature by feature** → `feature-specification.md`.
- **How the product is built and operated** → `technical-design.md`.

---

## 7. Business Model & Monetization

This is a pet project, so monetization is **intentionally minimal**.

| Aspect | Decision | Rationale |
|---|---|---|
| Platform fee | **None at launch.** EventHub is free to use. | Avoids financial, tax, and refund complexity; keeps the project simple and the value fair. |
| Free events | **Always free** to create and attend. | No reason to add cost where there is no money changing hands. |
| Paid events | Supported **through a trusted third-party payment provider**; EventHub itself **does not hold funds** and takes no cut. | The provider handles money safely; the builder avoids the responsibility of holding other people's funds. |
| Subscriptions / tiers | **None.** | Unnecessary at this scale. |
| Future option (deferred) | A small, transparent platform fee *could* be added later if the project ever needs to cover its own costs. Designed-for, but switched off. | Keeps the door open without committing now. |

---

## 8. Product Principles & Quality Guardrails

These are **outcome-level expectations**. The means of achieving them are defined in `technical-design.md`; the specific behaviors are defined in `feature-specification.md`.

| ID | Guardrail | Outcome expected |
|---|---|---|
| **QG-1** | Simplicity (highest priority) | Keep scope and UX simple; fewer features, done well; easy for one person to maintain. |
| **QG-2** | Transparency | Pricing is all-inclusive and shown upfront; no surprise charges at checkout. |
| **QG-3** | Fairness and trust | Tickets are valid and verifiable; transfers are fair (face value); the product does not enable scalping. |
| **QG-4** | Mobile-friendly | The experience is good on a phone, where most attendees will buy. |
| **QG-5** | Correct at small scale | Works reliably for the modest demand of a small event, degrades gracefully, and **never sells the same seat twice**. |
| **QG-6** | Responsible with data and money | Personal data and payments are handled with care, relying on trusted providers; respects basic applicable rules. |
| **QG-7** | Reasonable accessibility | Usable by people with a range of abilities. |
| **QG-8** | Light localization | Supports the builder's local language and at least one locally common payment method. |

---

## 9. Success Metrics & KPIs

**North Star:** the number of events successfully run end-to-end (tickets sold and attendees checked in).

| ID | Metric group | Example indicators |
|---|---|---|
| **KPI-1** | It works end-to-end | At least one real event run fully through the platform; tickets issued and scanned without errors; zero oversells. |
| **KPI-2** | Attendee experience | Checkout completion rate; time to complete a purchase; "the price was clear" (informal feedback). |
| **KPI-3** | Organizer experience | Time to set up an event; "I could run my event with this" (informal feedback). |
| **KPI-4** | Reliability | Error rate during an event's sale and at the door; no oversell incidents. |
| **KPI-5** | Adoption (modest) | Number of events / organizers that actually used it (for a pet project, even a handful is a success). |

---

## 10. Risks & Mitigations

| ID | Risk | Severity | Mitigation |
|---|---|---|---|
| **RSK-1** | Scope creep / over-engineering | High | Hold the line on simplicity (QG-1); ship a small MVP first (DEC-3); respect the non-goals list (6.2). |
| **RSK-2** | Solo maintenance burden / losing momentum | High | Keep scope small; choose a lean, low-maintenance stack (see technical-design.md); avoid infrastructure that is costly or hard to run alone. |
| **RSK-3** | Handling real money responsibly | Medium | Use a trusted payment provider and do not hold funds (DEC-1); take no platform fee at launch; keep refund handling simple and clear. |
| **RSK-4** | Little or no real usage | Low | Acceptable for a pet project; validate by running one real small event personally or with friends (DEC-4). |
| **RSK-5** | Overselling or double-issuing tickets | Medium | Correctness is a hard requirement (QG-5); the mechanism is defined in technical-design.md. |
| **RSK-6** | Personal-data and security responsibilities | Medium | Handle data with care and minimalism (QG-6); store only what is needed. |

---

## 11. Key Decisions, Assumptions & Dependencies

### 11.1. Key Decisions (resolved)
These resolve the questions that were previously open. They are intentional and revisable.

| ID | Question resolved | Decision | Rationale |
|---|---|---|---|
| **DEC-1** | How is it monetized? | No platform fee at launch; free to use; paid events run through a trusted payment provider; EventHub holds no funds; no subscriptions. | Avoids financial/tax/refund complexity; keeps it simple and fair; a small fee remains a deferred future option. |
| **DEC-2** | What is the resale / transfer policy? | **Face value only.** Support transferring a ticket to another person at no markup, and optionally returning a ticket to be re-sold at face value if the event is sold out. No paid resale marketplace. | Simplest to build and aligns with the fairness principle (QG-3); avoids enabling scalping. |
| **DEC-3** | What is in the first release vs later? | A **small MVP** that runs one small event end-to-end: create event → set up tickets (including free) → attendee buys via a shared link → ticket delivered → check-in at the door. Everything else is deferred. | Proves the core value with the least work; lets real use guide what comes next. |
| **DEC-4** | How are the first organizers found? | **Fully self-service; no sales effort.** The first events are the builder's own or friends'. | It is a personal project; the best validation is running a real event. |
| **DEC-5** | What is the timeline? | **No fixed deadlines.** Milestone-based, built in personal time; iterate after the MVP has run a real event. | A sustainable pace fits a pet project better than a schedule. |

#### Release sequencing (high-level)
Detailed feature breakdown lives in `feature-specification.md`; this is only the order of intent.

| Pillar (from 6.1) | MVP | Next | Later |
|---|---|---|---|
| Event creation & management | ✓ (basic) | richer options | — |
| Ticketing & transparent pricing | ✓ | discounts, ticket types | — |
| Payment & payout | ✓ (paid + free) | — | — |
| Attendee access & discovery | ✓ (shared link) | simple public listing / search | — |
| Purchase & ticket delivery | ✓ | — | — |
| Event-day check-in | ✓ | — | — |
| Basic audience care & results | — | attendee list + simple results | light messaging |
| Basic trust & fairness | ✓ (valid tickets, upfront price) | fair transfer | face-value return-to-pool |

### 11.2. Assumptions

| ID | Assumption |
|---|---|
| **ASM-1** | Built and maintained by a solo builder (or a very small team) in personal time. |
| **ASM-2** | Used for small events with modest concurrent demand — not large, high-pressure on-sales. |
| **ASM-3** | Most attendees buy on a phone. |
| **ASM-4** | Attendees prefer a transparent, all-inclusive price over fees revealed at checkout. |

### 11.3. Dependencies

| ID | Dependency |
|---|---|
| **DEP-1** | A trusted third-party payment provider (handles money; EventHub does not hold funds). |
| **DEP-2** | Basic, low-maintenance hosting suitable for a small project (detail in technical-design.md). |
| **DEP-3** | *(Optional)* A simple receipt / e-invoice mechanism, only if the builder needs tax-compliant receipts locally. |

---

## 12. Glossary

| Term | Meaning |
|---|---|
| **Attendee** | A person who discovers, buys, and attends events. |
| **Organizer** | A person or small group who creates and runs an event and sells tickets. |
| **Two-sided platform** | A product that serves and connects two interdependent groups (here, organizers and attendees). |
| **All-inclusive pricing** | A final price that already includes all fees, shown transparently from the start. |
| **Face value** | The original ticket price set by the organizer, with no markup. |
| **Scalping** | Acquiring tickets in order to resell them at inflated prices. |
| **No-show** | Someone who holds a ticket but does not attend. |
| **Check-in** | Validating a ticket and admitting the attendee at the door. |
| **MVP (Minimum Viable Product)** | The smallest version of the product that delivers the core value end-to-end. |
| **North Star** | The single measure that best captures the value the product delivers. |

---

*This PRD is a living draft for a personal project. Detailed features are maintained in `feature-specification.md`; technical design is maintained in `technical-design.md`. The decisions in Section 11 are intentional and may be revised as the project evolves.*
