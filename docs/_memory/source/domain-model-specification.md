---
document_type: domain_model_specification
methodology: Domain-Driven Design (strategic + tactical)
product_name: EventHub
version: "1.0"
status: draft
last_updated: "2026-06-14"
owner: Builder (solo / pet project)
language: en
companion_documents:
  product: product-requirements.md
  features: feature-specification.md
  technical: technical-design.md
  principles: constitution.md
identifier_scheme:
  bounded_context: "BC-<n>"
  aggregate: "AGG-<Name>"
  entity: "ENT-<Name>"
  value_object: "VO-<Name>"
  domain_event: "EVT-<Name>"
  invariant: "INV-<n>"
  domain_service: "SVC-<Name>"
  repository_port: "REPO-<Name>"
---

# EventHub — Domain Model Specification (`domain-model-specification.md`)

**The domain model: strategic boundaries and tactical building blocks.**

---

## 0. About this document (read first)

### 0.1 Purpose
This document specifies **EventHub's domain model** using Domain-Driven Design. It is the home of the model that `technical-design.md` deliberately leaves out. It defines the language, the boundaries (bounded contexts), and the building blocks (aggregates, entities, value objects, domain events, services, invariants).

### 0.2 Relation to the other documents
- `product-requirements.md` — *why/who/scope*. Domain rules here trace back to its decisions (`DEC-*`) and guardrails (`QG-*`).
- `feature-specification.md` — *what*, as observable behavior. Each feature (`F-*`) is realized by the model here.
- `technical-design.md` — *how it is built and run*. Persistence, concurrency, messaging, and ports are mechanisms; this document is the model those mechanisms serve.
- **This document** — *the model*: the structure and rules behind the behavior.

### 0.3 How to read this (humans and AI agents)
- **Identifiers** are stable and referenceable: `BC-<n>` bounded context, `AGG-<Name>` aggregate, `ENT-<Name>` entity, `VO-<Name>` value object, `EVT-<Name>` domain/integration event, `INV-<n>` invariant, `SVC-<Name>` domain service, `REPO-<Name>` repository port.
- **The Ubiquitous Language (§1) is authoritative.** Every other section uses exactly those terms; code and conversation should too.
- Cross-references such as `F-3.4`, `DEC-2`, or `technical-design.md §6` are intentional links.
- The model is for a **modular monolith** (per `technical-design.md`): bounded contexts are **logical modules** inside one Domain project, not separate services.

---

## 1. Ubiquitous Language

The shared vocabulary. The owning bounded context (§2.2) is shown for each term.

| Term | Meaning | Owner |
|------|---------|-------|
| **Organizer** | A person/account that creates and runs events and owns their audience data. | Identity & Access |
| **Attendee** | A person who buys/holds a ticket and attends. In the MVP an attendee is identified by **contact** (name + email), not an account. | Identity & Access |
| **Event** | A happening an organizer offers, with a schedule, a location (physical or online), and one or more ticket types. | Event Management |
| **Ticket Type** | A sellable category within an event (name, price, capacity), e.g. *General*, *VIP*. | Event Management |
| **Capacity** | The maximum number of a ticket type that may exist. | Event Management |
| **Inventory / Availability** | `Available = Capacity − Reserved − Sold` for a ticket type. | Event Management |
| **Reservation** | A time-limited hold on inventory created while an order is being paid. | Event Management |
| **Sold Out** | A ticket type whose availability has reached zero. | Event Management |
| **Order** | An attendee's purchase of one or more ticket types for an event. | Sales |
| **Order Line** | One ticket type and quantity within an order, with a **price snapshot**. | Sales |
| **Hold** | The window during which an order's reservation is valid before it must be paid. | Sales / Event Management |
| **Discount Code** | A code that lowers an order's total transparently at checkout. | Sales |
| **Face value** | The organizer-set price of a ticket; never marked up by EventHub. | Sales / Ticketing |
| **Payment** | A record of an attempt to settle an order through the external provider. EventHub holds no funds and stores no card data. | Payments |
| **Provider Reference** | The external provider's identifier for a payment, stored by EventHub. | Payments |
| **Ticket** | An issued admission with a unique code (QR), held by a contact, valid for one entry. | Ticketing |
| **Ticket Code** | The unique value encoded in a ticket's QR; admits exactly once. | Ticketing |
| **Check-in** | Validating a ticket at the door and admitting the holder, exactly once. | Ticketing |
| **Transfer** | Reassigning a ticket to another contact at face value (new code issued, old voided). | Ticketing |
| **Return-to-pool** | Handing a ticket back so it re-enters availability to be sold again at face value (with refund). | Ticketing → Event Management |
| **Attendee List / Results** | Read-only views of who is coming and how an event performed. | Reporting & Audience |
| **Notification** | An email sent to an attendee (tickets, reminders, updates, cancellations). | Notifications |

---

## 2. Strategic design

### 2.1 Subdomains

| Subdomain | Type | Why this classification | Bounded context |
|-----------|------|--------------------------|-----------------|
| Event & inventory | **Core** | Transparent pricing and the no-oversell guarantee are differentiators (`product-requirements.md` POS/QG). | Event Management (BC-2) |
| Selling | **Core** | The fair, all-inclusive checkout is central to the value proposition. | Sales (BC-3) |
| Admission & fair resale | **Core** | Valid tickets, smooth check-in, and face-value transfer are differentiators (`product-requirements.md` DEC-2). | Ticketing (BC-5) |
| Accounts & access | **Supporting** | Necessary, but not a differentiator; conventional. | Identity & Access (BC-1) |
| Audience & reporting | **Supporting** | Valuable to organizers but built from other contexts' data. | Reporting & Audience (BC-7) |
| Taking money | **Generic** | Delegated entirely to an external provider (`product-requirements.md` DEC-1). | Payments (BC-4) |
| Messaging | **Generic** | Email delivery is a commodity capability. | Notifications (BC-6) |

### 2.2 Bounded contexts

| ID | Context | Type | Responsibility | Aggregates |
|----|---------|------|----------------|------------|
| **BC-1** | Identity & Access | Supporting | Accounts, authentication, ownership identity | `User`, `Session` |
| **BC-2** | Event Management | Core | Events, ticket types, pricing, **inventory & reservations**, lifecycle | `Event` |
| **BC-3** | Sales | Core | Orders, holds, discounts, checkout, the purchase transaction | `Order` |
| **BC-4** | Payments | Generic | Settling orders via the external provider; refunds | `Payment` |
| **BC-5** | Ticketing | Core | Issued tickets, check-in, transfer, returns | `Ticket` |
| **BC-6** | Notifications | Generic | Sending emails driven by events | *(no aggregate; event-driven)* |
| **BC-7** | Reporting & Audience | Supporting | Read models: attendee lists, results | *(read projections; no aggregate)* |

### 2.3 Context map

Relationships use standard DDD patterns. Integration mechanism follows `technical-design.md`: **in-process domain events** within a context (one transaction, strong consistency); **integration events over RabbitMQ** across contexts (eventual consistency, idempotent consumers). One synchronous cross-context call is the exception, called out below.

| Upstream (Supplier) | Downstream (Customer) | Pattern | Integration |
|---------------------|------------------------|---------|-------------|
| Identity & Access (BC-1) | All other contexts | Published Language / Conformist | Other contexts reference `UserId`/`OrganizerId` by value; no translation. |
| Event Management (BC-2) | Sales (BC-3) | Customer/Supplier | **Synchronous** reserve call during checkout (strong consistency for no-oversell); commit/release and *sold-out* via integration events. |
| Sales (BC-3) | Payments (BC-4) | Customer/Supplier | Sales requests payment; Payments reports back — integration events. |
| External provider | Payments (BC-4) | **Anti-Corruption Layer** | Provider's model is translated at the boundary; webhook validated + made idempotent. |
| Sales (BC-3) | Ticketing (BC-5) | Customer/Supplier (Published Language) | `OrderConfirmed` integration event triggers issuance. |
| Event Management (BC-2) | Sales, Payments, Ticketing, Notifications | Open Host Service / Published Language | `EventCancelled` fans out as an integration event. |
| Ticketing (BC-5) | Event Management (BC-2) | Customer/Supplier | `TicketReturned` integration event returns inventory. |
| Ticketing / Sales | Payments (BC-4) | Customer/Supplier | Cancellation/return triggers refund — integration events. |
| Many contexts | Notifications (BC-6) | Published Language | Consumes events (e.g., `TicketIssued`, `EventCancelled`). |
| Many contexts | Reporting & Audience (BC-7) | Conformist | Subscribes to events to build read projections. |

---

## 3. Tactical design conventions

Rules that apply to every context. Mechanics (storage, transactions, concurrency) are in `technical-design.md`.

- **Aggregate = consistency boundary.** All invariants of an aggregate hold within a single transaction. Keep aggregates small.
- **Reference across aggregates/contexts by identity only** (e.g., an `Order` holds an `EventId`, never an `Event` object).
- **One repository per aggregate root** (`REPO-*`), expressed as an Application **port** and implemented in Infrastructure.
- **Value objects are immutable**, compared by value, and validate themselves on creation (an invalid value object cannot exist).
- **Domain events (`EVT-*`)** record something that happened. *Domain-scope* events are handled in-process within the same transaction's unit of work; *integration-scope* events are published to RabbitMQ for other contexts.
- **Factories** build aggregates whose creation is non-trivial (e.g., issuing tickets from a confirmed order).
- **Domain services (`SVC-*`)** hold domain logic that does not belong to a single entity (e.g., discount calculation). Cross-aggregate *orchestration* is an Application service, not a domain service.
- **No anemic models.** Behavior lives on the aggregate; the Application layer orchestrates, it does not encode business rules.

---

## 4. The model, by bounded context

### BC-1 — Identity & Access *(Supporting)*

#### AGG-User *(root)*
- **Identity:** `VO-UserId`
- **Value objects:** `VO-EmailAddress`, `VO-DisplayName`, `VO-PasswordHash`, `VO-UserRole` (`Organizer`; optionally `Attendee`)
- **Invariants:** unique email (`INV-1`); a password is only ever held as a hash (`INV-2`); email must be well-formed.
- **Behaviors:** `Register`, `ChangePassword`, `UpdateProfile`, `LinkAttendeeIdentity` *(optional, Later — F-1.4)*
- **Domain events:** `EVT-UserRegistered`
- **Repository:** `REPO-UserRepository`

#### AGG-Session *(root, thin)*
- **Identity:** `VO-SessionId`
- **Attributes:** `VO-UserId`, `ExpiresAt`
- **Invariants:** an expired session grants no access (`INV-3`).
- **Behaviors:** `Start`, `Invalidate`
- **Note:** mostly an infrastructure concern (cookie + cache, `technical-design.md` §5/§6); modeled here only for completeness.

> Other contexts treat the **attendee** as a `VO-Contact` (name + `VO-EmailAddress`), not a `User`. An optional account (F-1.4) merely links existing tickets by email.

---

### BC-2 — Event Management *(Core)*

This context owns **inventory** and the **no-oversell** invariant, which is why ticket types and reservations live inside the `Event` aggregate rather than in Sales.

#### AGG-Event *(root)*
- **Identity:** `VO-EventId`
- **Attributes:** `OrganizerId` (`VO-UserId`), `VO-EventTitle`, description, `VO-EventSchedule`, `VO-EventLocation`, `VO-Slug`, `VO-CoverImageRef`, `VO-EventStatus`
- **Entities:**
  - **ENT-TicketType** — `VO-TicketTypeId`, `VO-TicketName`, `VO-Money Price`, `VO-Capacity`, `Sold`, `Reserved`, optional `VO-SalesWindow`, optional `MaxPerOrder`.
  - **ENT-Reservation** — `VO-ReservationId`, `TicketTypeId`, `Quantity`, `ExpiresAt`, `OrderId` (back-reference).
- **Value objects:** `VO-EventSchedule` (start/end + time zone), `VO-EventLocation` (physical address or `Online`), `VO-Slug`, `VO-CoverImageRef`, `VO-EventStatus` (`Draft`/`Published`/`Closed`/`Cancelled`), `VO-Money`, `VO-Capacity`, `VO-SalesWindow`.
- **Invariants:** `INV-10` no-oversell (`Reserved + Sold ≤ Capacity` per ticket type); `INV-11` publishable only with required details **and** ≥1 ticket type; `INV-12` capacity cannot drop below `Reserved + Sold`; `INV-13` price ≥ 0; `INV-14` cannot reserve unless `Published`, within `SalesWindow`, and not `Closed`/`Cancelled`; `INV-15` slug unique among published events.
- **Behaviors:** `CreateDraft`, `UpdateDetails`, `AddTicketType`, `ChangeTicketType` (guarded), `SetCoverImage`, `Publish`, `Close`, `Cancel`, `Reserve(ticketTypeId, qty) → Reservation`, `ReleaseReservation(reservationId)`, `CommitReservation(reservationId)` (reserved → sold), `ReturnToPool(ticketTypeId, qty)`.
- **Domain events:** `EVT-EventPublished`, `EVT-EventClosed`, `EVT-EventCancelled`, `EVT-TicketTypeAdded`, `EVT-InventoryReserved`, `EVT-ReservationReleased`, `EVT-ReservationCommitted`, `EVT-InventoryReturnedToPool`, `EVT-EventSoldOut`
- **Repository:** `REPO-EventRepository`
- **Realizes:** F-2.*, F-3.* · **Hot-aggregate note** in §8.

---

### BC-3 — Sales *(Core)*

#### AGG-Order *(root)*
- **Identity:** `VO-OrderId`
- **Attributes:** `EventId`, `VO-Contact` (buyer name + email — guest), `VO-OrderStatus`, `VO-Money Total`, optional applied `VO-DiscountCode`, `ReservationId` (from BC-2), `PaymentId` (from BC-4), `PlacedAt`, `HoldExpiresAt`, `ConfirmedAt`
- **Entities:** **ENT-OrderLine** — `TicketTypeId`, `Quantity`, **`VO-Money UnitPriceSnapshot`**, `LineTotal`.
- **Value objects:** `VO-Contact`, `VO-OrderStatus` (`Pending`/`Confirmed`/`Expired`/`Cancelled`/`Refunded`), `VO-Money`, `VO-DiscountCode`.
- **Invariants:** `INV-20` `Total = Σ line totals − discount`, and `Total ≥ 0`; `INV-21` a `Pending` order must reference a live reservation; `INV-22` cannot confirm an `Expired`/`Cancelled` order; `INV-23` confirmation requires a captured payment **or** a zero total (free); `INV-24` line quantities respect `MaxPerOrder` at placement; `INV-25` **prices are snapshotted** at placement so later price changes never alter a placed order.
- **Behaviors:** `Place` (snapshot prices, attach reservation), `ApplyDiscount`, `MarkConfirmed`, `Expire`, `Cancel`, `MarkRefunded`.
- **Domain services:** `SVC-DiscountPolicy` (validates a code and computes the reduction).
- **Domain events:** `EVT-OrderPlaced`, `EVT-OrderConfirmed`, `EVT-OrderExpired`, `EVT-OrderCancelled`, `EVT-OrderRefunded`
- **Repository:** `REPO-OrderRepository`
- **Realizes:** F-5.*, F-3.6, F-3.7.

---

### BC-4 — Payments *(Generic; integrates the external provider via an ACL)*

#### AGG-Payment *(root)*
- **Identity:** `VO-PaymentId`
- **Attributes:** `OrderId`, `VO-Money Amount`, `VO-PaymentStatus`, `VO-ProviderReference`, timestamps
- **Value objects:** `VO-PaymentStatus` (`Initiated`/`Captured`/`Failed`/`Refunded`), `VO-ProviderReference`, `VO-Money`.
- **Invariants:** `INV-30` amount equals the order total; `INV-31` valid status transitions only; `INV-32` **capture is idempotent** — a provider callback is applied at most once; `INV-33` **no card data is ever stored**, only status + provider reference + amount (`product-requirements.md` QG-6, DEC-1).
- **Behaviors:** `Initiate`, `Capture`, `Fail`, `Refund`.
- **Anti-Corruption Layer:** the `IPaymentGateway` port (`technical-design.md` §4) translates the provider's model to `Payment`; the webhook is signature-verified and de-duplicated at the boundary.
- **Domain events:** `EVT-PaymentInitiated`, `EVT-PaymentCaptured`, `EVT-PaymentFailed`, `EVT-PaymentRefunded`
- **Repository:** `REPO-PaymentRepository`
- **Realizes:** F-6.*.

---

### BC-5 — Ticketing *(Core)*

#### AGG-Ticket *(root)*
- **Identity:** `VO-TicketId`
- **Attributes:** `EventId`, `OrderId` (origin), `TicketTypeId`, `VO-TicketCode`, `VO-Contact` (holder), `VO-TicketStatus`, `CheckedInAt?`, `transferredFrom?`
- **Value objects:** `VO-TicketCode` (unique; QR payload), `VO-TicketStatus` (`Valid`/`CheckedIn`/`Transferred`/`Void`), `VO-Contact`.
- **Invariants:** `INV-40` a ticket code admits **exactly once** (idempotent check-in); `INV-41` check-in only on a `Valid` ticket for the matching event; `INV-42` **transfer is face value only** — there is no price on transfer; a transfer voids the old ticket and issues a new one with a new code; `INV-43` a `CheckedIn` ticket cannot be transferred or returned; `INV-44` a `Void`/`Transferred` ticket cannot be checked in.
- **Behaviors:** `CheckIn` (idempotent), `Transfer(toContact)`, `Void`, `Return`.
- **Factory:** `SVC-TicketFactory` issues one ticket per purchased unit from a confirmed order, generating unique codes.
- **Domain service:** `SVC-TicketCodeGenerator` (collision-free codes).
- **Domain events:** `EVT-TicketIssued`, `EVT-TicketCheckedIn`, `EVT-TicketTransferred`, `EVT-TicketVoided`, `EVT-TicketReturned`
- **Repository:** `REPO-TicketRepository`
- **Realizes:** F-7.*, F-8.*, F-10.*.

---

### BC-6 — Notifications *(Generic; event-driven, no aggregate)*
Consumes integration events and sends emails through the `IEmailSender` port (`technical-design.md` §5), asynchronously via RabbitMQ.
- **Triggers → action:** `TicketIssued` → deliver tickets (F-7.2); `EventCancelled` → notify attendees (F-2.5); scheduled reminders (F-9.6); organizer messages (F-9.5).
- **Note:** holds no business invariants; delivery is at-least-once and idempotent per recipient+message.

### BC-7 — Reporting & Audience *(Supporting; read side, no aggregate)*
CQRS read models, projected from domain/integration events; owned by the organizer (`product-requirements.md` G-5).
- **Projections:** *Attendee list* (from `OrderConfirmed`, `TicketIssued`, `TicketCheckedIn`) → F-9.1/F-9.2; *Event results* (sold by type, revenue = gross since no platform fee, check-in rate, no-shows) → F-9.3; *Organizer overview* → F-9.4.
- **Consistency:** eventually consistent with the write side.

---

## 5. Value object catalogue (notable / shared)

| VO | Used in | Rules |
|----|---------|-------|
| `VO-Money` | BC-2, BC-3, BC-4 | Amount + a **single configured currency**; non-negative; arithmetic only within the same currency. Multi-currency is a non-goal (`product-requirements.md` §6.2). |
| `VO-EmailAddress` | BC-1, BC-3, BC-5 | Well-formed; normalized for comparison. |
| `VO-Contact` | BC-3, BC-5 | Name + `VO-EmailAddress`; how a guest attendee is identified. |
| `VO-EventSchedule` | BC-2 | Start/end with time zone; end ≥ start. |
| `VO-EventLocation` | BC-2 | Either a physical address or `Online`; exactly one. |
| `VO-Capacity` | BC-2 | Positive integer; the ceiling for a ticket type. |
| `VO-Slug` | BC-2 | URL-safe; unique among published events. |
| `VO-CoverImageRef` | BC-2 | Object key/URL into object storage (`technical-design.md` §5); never the bytes. |
| `VO-TicketCode` | BC-5 | Unique, unguessable; the QR payload. |
| `VO-ProviderReference` | BC-4 | The external provider's payment identifier. |
| Typed IDs | all | `VO-UserId`, `VO-EventId`, `VO-TicketTypeId`, `VO-ReservationId`, `VO-OrderId`, `VO-PaymentId`, `VO-TicketId` — identity is never a bare primitive. |

---

## 6. Domain & integration events

| EVT | Scope | Raised by | Trigger | Consumers |
|-----|-------|-----------|---------|-----------|
| `EVT-UserRegistered` | domain | AGG-User | account created | — |
| `EVT-EventPublished` | integration | AGG-Event | event published | Reporting (BC-7) |
| `EVT-InventoryReserved` | domain | AGG-Event | reservation created | — |
| `EVT-ReservationReleased` | integration | AGG-Event | hold expired / released | Sales (expire order) |
| `EVT-ReservationCommitted` | domain | AGG-Event | order confirmed | — |
| `EVT-EventSoldOut` | integration | AGG-Event | availability hit 0 | Realtime/Reporting (F-11.3) |
| `EVT-OrderPlaced` | domain | AGG-Order | order placed | — |
| `EVT-OrderConfirmed` | integration | AGG-Order | payment captured / free | Ticketing (issue), Event Mgmt (commit), Reporting |
| `EVT-OrderExpired` | domain | AGG-Order | hold elapsed | — |
| `EVT-PaymentInitiated` | domain | AGG-Payment | checkout started | — |
| `EVT-PaymentCaptured` | integration | AGG-Payment | provider webhook | Sales (confirm order) |
| `EVT-PaymentFailed` | domain | AGG-Payment | provider failure | Sales |
| `EVT-PaymentRefunded` | integration | AGG-Payment | refund processed | Sales, Ticketing |
| `EVT-TicketIssued` | integration | AGG-Ticket | order confirmed | Notifications (deliver), Reporting |
| `EVT-TicketCheckedIn` | integration | AGG-Ticket | scanned at door | Realtime (F-11.2), Reporting |
| `EVT-TicketTransferred` | integration | AGG-Ticket | transfer completed | Reporting |
| `EVT-TicketReturned` | integration | AGG-Ticket | return-to-pool | Event Mgmt (return inventory), Payments (refund) |
| `EVT-EventCancelled` | integration | AGG-Event | event cancelled | Sales, Payments, Ticketing, Notifications |

---

## 7. Lifecycles (state transitions)

**AGG-Event status**

| From | Action | To | Guard |
|------|--------|----|-------|
| — | CreateDraft | Draft | — |
| Draft | Publish | Published | required details + ≥1 ticket type (`INV-11`) |
| Published | Close | Closed | — |
| Published / Closed | Cancel | Cancelled | triggers refunds (F-6.6) |

**AGG-Order status**

| From | Action | To | Guard |
|------|--------|----|-------|
| — | Place | Pending | reservation held (`INV-21`) |
| Pending | MarkConfirmed | Confirmed | payment captured or zero total (`INV-23`) |
| Pending | Expire | Expired | hold elapsed |
| Pending / Confirmed | Cancel | Cancelled | — |
| Confirmed | MarkRefunded | Refunded | refund processed |

**AGG-Payment status:** `Initiated → Captured`, `Initiated → Failed`, `Captured → Refunded` (capture idempotent, `INV-32`).

**AGG-Ticket status:** `Valid → CheckedIn` (once, `INV-40`); `Valid → Transferred` (issues a new `Valid` ticket); `Valid → Void` (refund/cancel/return).

---

## 8. Consistency & transaction boundaries

- **Strong consistency lives inside an aggregate.** The no-oversell rule (`INV-10`) is a true `Event` invariant: `Reserve`, `ReleaseReservation`, `CommitReservation`, and `ReturnToPool` all mutate the `Event` within one transaction, guarded by **optimistic concurrency + retry** (`technical-design.md` §6). Two buyers racing for the last ticket → exactly one wins (`F-3.4`).
- **Hot-aggregate trade-off.** Because inventory lives in `Event`, the aggregate is a concurrency hot spot. At this project's scale that is acceptable and is exactly what optimistic concurrency handles; a known scaling option (out of scope) is to split inventory into its own aggregate/context.
- **Pragmatic two-aggregate write.** `PlaceOrder` reserves on `Event` (BC-2) **and** creates the `Order` (BC-3) in a single transaction. This bends the "one aggregate per transaction" guideline deliberately, justified by the monolith + single database + the strong no-oversell requirement + simplicity (`product-requirements.md` QG-1). All *other* cross-context interactions are eventual.
- **Eventual consistency across contexts.** Everything else flows via **integration events on RabbitMQ** with **idempotent consumers** (`technical-design.md` §4/§5). Example: tickets are issued shortly after confirmation (`F-7.2`), not in the same transaction.
- **Price snapshotting.** Order lines snapshot price at placement (`INV-25`) so transparent pricing (`F-3.3`) is honored even if the organizer later edits prices.
- **Idempotency keys** protect the two retried boundaries: the payment webhook (`INV-32`) and ticket issuance on `OrderConfirmed`.

---

## 9. End-to-end domain flow (purchase → attend)

How the contexts connect for the happy path and the fair-resale paths. Each step cites the feature it realizes.

| # | Context · Aggregate | Action | Emits | Consumed by | Feature |
|---|---------------------|--------|-------|-------------|---------|
| 1 | Event Mgmt · Event | Publish event | `EventPublished` | Reporting | F-2.4 |
| 2 | Sales (app) | PlaceOrder: `Event.Reserve` + create `Order` (one tx) | `InventoryReserved`, `OrderPlaced` | — | F-5.1–F-5.4 |
| 3a | Payments · Payment | Initiate (paid) | `PaymentInitiated` | — | F-6.1 |
| 3b | Sales · Order | Confirm directly (free, total = 0) | `OrderConfirmed` | Ticketing, Event Mgmt, Reporting | F-6.2 |
| 4 | Payments · Payment | Capture via webhook (idempotent) | `PaymentCaptured` | Sales | F-6.3 |
| 5 | Sales · Order | MarkConfirmed | `OrderConfirmed` | Ticketing, Event Mgmt, Reporting | F-6.3 |
| 6 | Event Mgmt · Event | CommitReservation (reserved → sold) | `ReservationCommitted` | — | F-3.4 |
| 7 | Ticketing · Ticket | Issue one ticket per unit | `TicketIssued` | Notifications, Reporting | F-7.1 |
| 8 | Notifications | Email tickets (async) | — | — | F-7.2 |
| 9 | Ticketing · Ticket | Check-in at door (idempotent) | `TicketCheckedIn` | Reporting, Realtime | F-8.1, F-8.2 |
| — | Ticketing · Ticket | **Transfer** (face value): void old, issue new | `TicketTransferred`, `TicketIssued`, `TicketVoided` | Reporting, Notifications | F-10.1 |
| — | Ticketing · Ticket | **Return-to-pool** (sold out): void ticket | `TicketReturned` | Event Mgmt (return inventory), Payments (refund) | F-10.3 |
| — | Sales · Order | Hold expires (no payment) | `OrderExpired` ← `ReservationReleased` | Event Mgmt (inventory returns) | F-5.5, F-6.4 |
| — | Event Mgmt · Event | **Cancel event** | `EventCancelled` | Sales, Payments (refund), Ticketing (void), Notifications | F-2.5, F-6.6 |

---

## 10. Mapping to features and technical design

| Bounded context · Aggregate | Epics (`feature-specification.md`) | Technical anchors (`technical-design.md`) |
|-----------------------------|------------------------|-------------------------------------|
| BC-1 Identity & Access · `User`, `Session` | EP-1 | Cookie session auth, `ICurrentUserAccessor` (§7) |
| BC-2 Event Management · `Event` (+ `TicketType`, `Reservation`) | EP-2, EP-3 | Optimistic concurrency / no-oversell (§6); cover image in object storage (§5) |
| BC-3 Sales · `Order` (+ `OrderLine`) | EP-5 (+ F-3.6/3.7) | CQRS commands, unit-of-work transaction (§4) |
| BC-4 Payments · `Payment` | EP-6 | `IPaymentGateway` ACL + webhook idempotency (§4) |
| BC-5 Ticketing · `Ticket` | EP-7, EP-8, EP-10 | Idempotent check-in; codes/QR |
| BC-6 Notifications | F-7.2, F-9.5, F-9.6 | `IEmailSender` + RabbitMQ (§5) |
| BC-7 Reporting & Audience | EP-9 | CQRS read models / projections (§4) |
| Discovery (read of BC-2) | EP-4 | Public event query (§7) |
| Realtime (reads BC-2/3/5 events) | EP-11 | SignalR hubs (§5) |

---

*This document is a living draft for a personal project. It specifies EventHub's domain model; behavior is in `feature-specification.md`, intent is in `product-requirements.md`, and build/run mechanics are in `technical-design.md`. Boundaries, aggregates, and invariants may be revised as the model is learned in practice — that revision is itself part of DDD.*
