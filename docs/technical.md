---
doc_schema: eventhub-doc-v1
doc_kind: source_spec
doc_id: eventhub.technical
title: EventHub Technical Specification
status: active
version: "3.0"
last_updated: 2026-07-11
owner: builder
language: en
authority: architecture_domain_model_data_integrations_operations_and_verification
source_documents:
  - docs/product.md
  - docs/features.md
---

# EventHub — Technical Specification (`technical.md`)

**How EventHub is modeled, built, integrated, operated, and verified.**

---

## 0. Specification contract

### 0.1 Authority

This document is the authoritative source for:

- architecture and dependency rules;
- domain language, bounded contexts, aggregates, invariants, and state transitions;
- application flow, persistence, concurrency, messaging, and external integrations;
- API, security, configuration, observability, local runtime, and verification strategy.

Product outcomes and scope belong to [`product.md`](product.md). Observable behavior and acceptance criteria belong to [`features.md`](features.md).

### 0.2 Conformance

An implementation conforms only when:

1. it satisfies the relevant `F-*` acceptance criteria;
2. it preserves every applicable `INV-*` invariant;
3. it follows the normative `ARCH-*` rules or records an explicit replacement decision in this document;
4. it has verification proportional to the risk of the behavior.

### 0.3 Normative language and change rule

`MUST`, `MUST NOT`, `SHOULD`, `SHOULD NOT`, and `MAY` are normative. A technical change that modifies user-visible behavior **MUST** update `features.md`; a change that alters scope or a product decision **MUST** update `product.md` first.

---

## 1. Architectural drivers

| Driver | Product source | Technical response |
|---|---|---|
| Small, maintainable solo project | `G-2`, `QG-1`, `ASM-1` | Modular monolith, one deployable API host, conventional components, no microservice split. |
| No oversell | `QG-5`, F-3.4 | Inventory is protected by an aggregate invariant, database transaction, optimistic concurrency, and retry. |
| Transparent pricing | `G-3`, `QG-2`, F-3.3/F-5.4 | Money is a value object; order lines snapshot prices; charged amount must equal displayed final total. |
| Responsible payments | `QG-6`, `DEC-1`, EP-6 | Provider anti-corruption layer, signed/idempotent webhook handling, no card storage, no funds held by EventHub. |
| Reliable ticket admission | `QG-3`, F-7.1/F-8.1/F-8.2 | Unique unguessable ticket codes and idempotent exactly-once admission semantics. |
| Mobile and realtime UX | `QG-4`, EP-4/EP-11 | REST for commands/queries; SignalR only for server-push enhancements. |

## 2. Architecture

### 2.1 Architectural style

EventHub is a **modular monolith** using:

- **Clean Architecture** for dependency direction;
- **Domain-Driven Design** for business boundaries and invariants;
- **CQRS** for distinct command and query models over one PostgreSQL source of truth;
- **ports and adapters** for external systems;
- **event-driven integration** between logical bounded contexts where immediate consistency is not required.

### 2.2 Normative architecture rules

- **ARCH-1 — Dependency direction:** `Domain <- Application <- Infrastructure`, with `Api` as composition root. Inner layers MUST NOT reference outer layers.
- **ARCH-2 — Pure domain:** Domain MUST remain pure C# and MUST NOT depend on EF Core, ASP.NET Core, MediatR, Redis, RabbitMQ, MinIO, SignalR, or Infrastructure.
- **ARCH-3 — Application ownership:** Commands, queries, validators, orchestration, and external-system ports belong to Application.
- **ARCH-4 — Adapter ownership:** Infrastructure implements persistence and external-system ports. Api owns HTTP, authentication middleware, endpoints, and hubs.
- **ARCH-5 — Thin transport:** Endpoints MUST bind/authorize/dispatch/map only; business rules MUST NOT be implemented in endpoints.
- **ARCH-6 — Source of truth:** PostgreSQL is authoritative. Redis and read projections MUST be rebuildable.
- **ARCH-7 — Contract first:** Public REST shapes are governed by `contracts/openapi/api.v1.yaml`; generated clients MUST NOT be edited manually.
- **ARCH-8 — Idempotent boundaries:** Payment callbacks, message consumers, ticket issuance, and check-in MUST tolerate retries safely.
- **ARCH-9 — Single deployable:** Bounded contexts are logical modules, not independently deployed services, unless this specification is deliberately revised.

### 2.3 Logical request flow

`Api → Application → Domain`, with Infrastructure invoked through Application ports. Infrastructure may reference Application and Domain, while Domain is unaware of all outer mechanisms. Cross-cutting telemetry and health are configured through `ServiceDefaults`.

## 3. Repository structure and layer rules

```text
src/
  AppHost/           .NET Aspire orchestration
  ServiceDefaults/   logging, telemetry, health, service discovery
  Api/               HTTP host, endpoints, auth middleware, SignalR hubs
  Application/       commands, queries, validators, behaviors, ports
  Domain/            aggregates, entities, value objects, domain events
  Infrastructure/    EF Core, cache, storage, messaging, repositories, adapters
  Contracts/         HTTP request/response DTOs
tests/
  Domain.UnitTests/
  Api.IntegrationTests/
  Testing.Common/
```

| Layer | May reference | MUST NOT reference |
|---|---|---|
| Domain | — | EF Core, ASP.NET Core, MediatR, Infrastructure |
| Application | Domain, Contracts | Infrastructure, HTTP transport |
| Infrastructure | Application, Domain | Api |
| Api | Application, Infrastructure, Contracts | business rules in endpoints |

Every project SHOULD expose `AssemblyReference.Assembly` for assembly scanning and registration.

## 4. Application architecture

### 4.1 CQRS contracts

Commands implement `ICommand` / `ICommand<T>`; queries implement `IQuery<T>`. Handlers return `Result` or `Result<T>`. Commands mutate state; queries MUST be side-effect free.

### 4.2 MediatR pipeline

Registration order is outermost to innermost:

1. `LoggingBehavior` — correlation, timing, and structured request context.
2. `ValidationBehavior` — FluentValidation before any state mutation.
3. `PostCommitSessionCacheBehavior` — updates cache only after the inner unit of work has committed successfully.
4. `UnitOfWorkBehavior` — transaction boundary and bounded optimistic-concurrency retry for commands.
5. `DomainEventDispatchBehavior` — dispatches in-process domain events after the handler and before transaction commit.
6. Handler.

Queries bypass transaction and retry behavior unless a specific query requires a consistent snapshot. Domain-event handlers running inside the unit of work MUST remain local, deterministic, and free of slow external side effects.

### 4.3 Cross-context orchestration

Application services coordinate aggregates and contexts. Domain services contain business logic only when it does not naturally belong to one entity or aggregate. Application orchestration MUST NOT duplicate aggregate invariants.

## 5. Ubiquitous language

| Term | Meaning | Owner |
|---|---|---|
| Organizer | Account that creates and runs events and owns its audience data. | BC-1 |
| Attendee | Person who buys or holds a ticket. In the MVP, identified by contact name and email rather than requiring an account. | Shared identity concept |
| Event | Organizer-owned occurrence with schedule, location, and ticket types. | BC-2 |
| Ticket Type | Sellable category with price, capacity, availability, and optional sales rules. | BC-2 |
| Capacity | Maximum quantity for a ticket type. | BC-2 |
| Availability | `Capacity − Reserved − Sold`. | BC-2 |
| Reservation | Time-limited inventory hold associated with an order. | BC-2 |
| Order | Attendee purchase containing price-snapshotted order lines. | BC-3 |
| Hold | Time window in which a pending order's reservation remains valid. | BC-2/BC-3 |
| Payment | Attempt to settle an order through the external provider. | BC-4 |
| Ticket | Issued admission represented by a unique code and held by a contact. | BC-5 |
| Check-in | Validation and admission of a valid ticket exactly once. | BC-5 |
| Transfer | Reassignment of a ticket with no markup; old code is invalidated and a new code is issued. | BC-5 |
| Return-to-pool | Voiding an eligible ticket, refunding it, and restoring inventory for face-value resale. | BC-5 → BC-2 |
| Attendee List / Results | Organizer-owned read models derived from sales, tickets, and check-ins. | BC-7 |
| Notification | Email initiated by a domain/integration event or scheduled action. | BC-6 |

## 6. Strategic domain design

### 6.1 Subdomains and bounded contexts

| ID | Context | Type | Responsibility | Aggregate or shape |
|---|---|---|---|---|
| BC-1 | Identity & Access | Supporting | Accounts, authentication identity, ownership and event roles | `AGG-User`; sessions are an application/infrastructure concern |
| BC-2 | Event Management | Core | Events, ticket types, pricing, inventory, reservations, lifecycle | `AGG-Event` |
| BC-3 | Sales | Core | Orders, holds, discounts, checkout, price snapshots | `AGG-Order` |
| BC-4 | Payments | Generic | Provider payment/refund state behind an anti-corruption layer | `AGG-Payment` |
| BC-5 | Ticketing | Core | Ticket issuance, access, check-in, transfer, returns | `AGG-Ticket` |
| BC-6 | Notifications | Generic | Idempotent asynchronous email delivery | Event-driven; no aggregate |
| BC-7 | Reporting & Audience | Supporting | Attendee and results projections | Read models; no aggregate |

### 6.2 Context map

| Upstream | Downstream | Relationship and mechanism |
|---|---|---|
| BC-1 | All contexts | Published identity values (`UserId`, `OrganizerId`); downstream contexts reference identity only. |
| BC-2 | BC-3 | Synchronous reservation during order placement to protect inventory; release/commit outcomes are coordinated transactionally or by idempotent events as specified below. |
| BC-3 | BC-4 | Sales requests payment; Payments reports captured/failed/refunded outcomes. |
| External provider | BC-4 | Anti-corruption layer validates signatures and translates provider concepts. |
| BC-3 | BC-5 | `EVT-OrderConfirmed` triggers ticket issuance. |
| BC-2 | BC-3/4/5/6 | `EVT-EventCancelled` fans out to cancellation, refund, voiding, and notification workflows. |
| BC-5 | BC-2/4 | Return-to-pool restores inventory and requests refund. |
| BC-2/3/4/5 | BC-6/7 | Integration events drive notifications and projections. |

### 6.3 Tactical conventions

- An aggregate is a consistency boundary; its invariants MUST hold at transaction commit.
- Aggregates and contexts MUST reference one another by typed identity, never object graph.
- Each aggregate root has one Application repository port implemented by Infrastructure.
- Value objects are immutable, self-validating, and compared by value.
- Domain events describe facts that occurred. Integration events are durable messages for other contexts.
- Aggregate creation that requires coordination MAY use a domain factory.
- Cross-aggregate workflow belongs to Application, not a domain service.
- The domain MUST be behavioral; application handlers MUST NOT become the primary home of business rules.

## 7. Tactical domain model

### 7.1 BC-1 — Identity & Access

#### AGG-User

- **Identity:** `VO-UserId`.
- **Value objects:** `VO-EmailAddress`, `VO-DisplayName`, `VO-PasswordHash`.
- **Invariants:**
  - `INV-1`: organizer email is unique.
  - `INV-2`: plaintext passwords are never persisted; the domain receives or stores only an approved hash representation.
  - `INV-3`: an expired or invalidated session grants no access; this is enforced by the authentication adapter, not modeled as a separate domain aggregate.
- **Behaviors:** `Register`, `ChangePassword`, `UpdateProfile`, optional `LinkAttendeeIdentity`.
- **Events:** `EVT-UserRegistered`.
- **Repository:** `REPO-UserRepository`.
- **Features:** EP-1.

Event-scoped Owner/Staff assignments and permission checks belong to the Identity & Access module even if persisted in dedicated entities or tables. There MUST be exactly one Owner per event as required by F-1.6.

### 7.2 BC-2 — Event Management

#### AGG-Event

- **Identity:** `VO-EventId`.
- **Attributes:** organizer identity, title, description, schedule, location, slug, cover-image reference, status.
- **Entities:**
  - `ENT-TicketType`: id, name, `VO-Money` price, `VO-Capacity`, sold, reserved, optional sales window, optional max-per-order.
  - `ENT-Reservation`: id, ticket type, quantity, expiry, associated order identity.
- **Status:** `Draft`, `Published`, `Closed`, `Cancelled`.
- **Invariants:**
  - `INV-10`: `Reserved + Sold <= Capacity` for every ticket type.
  - `INV-11`: an event can publish only with required details and at least one ticket type.
  - `INV-12`: capacity cannot be reduced below `Reserved + Sold`.
  - `INV-13`: price is non-negative.
  - `INV-14`: reservation is allowed only while the event is Published, within the sales window, and with sufficient availability.
  - `INV-15`: published-event slug is unique.
- **Behaviors:** `CreateDraft`, `UpdateDetails`, `AddTicketType`, `ChangeTicketType`, `SetCoverImage`, `Publish`, `Close`, `Cancel`, `Reserve`, `ReleaseReservation`, `CommitReservation`, `ReturnToPool`.
- **Events:** `EVT-EventPublished`, `EVT-EventClosed`, `EVT-EventCancelled`, `EVT-TicketTypeAdded`, `EVT-InventoryReserved`, `EVT-ReservationReleased`, `EVT-ReservationCommitted`, `EVT-InventoryReturnedToPool`, `EVT-EventSoldOut`.
- **Repository:** `REPO-EventRepository`.
- **Features:** EP-2, EP-3.

### 7.3 BC-3 — Sales

#### AGG-Order

- **Identity:** `VO-OrderId`.
- **Attributes:** event identity, buyer `VO-Contact`, status, total, optional discount, reservation identity, optional payment identity, placed/expiry/confirmation timestamps.
- **Entity:** `ENT-OrderLine` with ticket-type identity, quantity, unit-price snapshot, and line total.
- **Status:** `Pending`, `Confirmed`, `Expired`, `Cancelled`, `Refunded`.
- **Invariants:**
  - `INV-20`: `Total = sum(line totals) − discount`, and total is never negative.
  - `INV-21`: a Pending order references a live reservation.
  - `INV-22`: Expired or Cancelled orders cannot be confirmed.
  - `INV-23`: confirmation requires captured payment or zero total.
  - `INV-24`: quantities respect the configured per-order limit at placement.
  - `INV-25`: unit prices are snapshotted at placement and never retroactively changed.
- **Behaviors:** `Place`, `ApplyDiscount`, `MarkConfirmed`, `Expire`, `Cancel`, `MarkRefunded`.
- **Domain service:** `SVC-DiscountPolicy`.
- **Events:** `EVT-OrderPlaced`, `EVT-OrderConfirmed`, `EVT-OrderExpired`, `EVT-OrderCancelled`, `EVT-OrderRefunded`.
- **Repository:** `REPO-OrderRepository`.
- **Features:** EP-5, F-3.6, F-3.7.

### 7.4 BC-4 — Payments

#### AGG-Payment

- **Identity:** `VO-PaymentId`.
- **Attributes:** order identity, amount, status, provider reference, timestamps.
- **Status:** `Initiated`, `Captured`, `Failed`, `Refunded`.
- **Invariants:**
  - `INV-30`: payment amount equals the order total.
  - `INV-31`: only valid payment state transitions are allowed.
  - `INV-32`: provider capture callbacks are applied at most once.
  - `INV-33`: EventHub stores no card data and holds no funds; only amount, status, and provider references are retained.
- **Behaviors:** `Initiate`, `Capture`, `Fail`, `Refund`.
- **Port:** `IPaymentGateway` translates the provider model and verifies callbacks.
- **Events:** `EVT-PaymentInitiated`, `EVT-PaymentCaptured`, `EVT-PaymentFailed`, `EVT-PaymentRefunded`.
- **Repository:** `REPO-PaymentRepository`.
- **Features:** EP-6.

### 7.5 BC-5 — Ticketing

#### AGG-Ticket

- **Identity:** `VO-TicketId`.
- **Attributes:** event, order and ticket-type identities; `VO-TicketCode`; holder contact; status; optional check-in and transfer origin.
- **Status:** `Valid`, `CheckedIn`, `Transferred`, `Void`.
- **Invariants:**
  - `INV-40`: a ticket code admits exactly once; repeated identical check-in requests are idempotent.
  - `INV-41`: only a Valid ticket for the matching event may be checked in.
  - `INV-42`: transfer has no markup or transfer price; old ticket is invalidated and a new code is issued.
  - `INV-43`: a CheckedIn ticket cannot be transferred or returned.
  - `INV-44`: Void or Transferred tickets cannot be checked in.
- **Behaviors:** `CheckIn`, `Transfer`, `Void`, `Return`.
- **Services:** `SVC-TicketFactory`, `SVC-TicketCodeGenerator`.
- **Events:** `EVT-TicketIssued`, `EVT-TicketCheckedIn`, `EVT-TicketTransferred`, `EVT-TicketVoided`, `EVT-TicketReturned`.
- **Repository:** `REPO-TicketRepository`.
- **Features:** EP-7, EP-8, EP-10.

### 7.6 BC-6 — Notifications

No aggregate. Consumers send email through `IEmailSender` in response to ticket issuance, event cancellation, reminders, invitations, and organizer messages. Delivery is at-least-once and MUST be idempotent for a logical `(message, recipient)` pair.

### 7.7 BC-7 — Reporting & Audience

No write aggregate. CQRS projections produce attendee lists, event results, and organizer overviews from order, ticket, and check-in events. Projections are eventually consistent and MUST be rebuildable from authoritative PostgreSQL data and/or retained integration events.

## 8. Shared value objects

| Value object | Rules |
|---|---|
| `VO-Money` | Non-negative amount plus one configured currency; arithmetic only within the same currency. |
| `VO-EmailAddress` | Well-formed and normalized for comparison. |
| `VO-Contact` | Name plus normalized email; guest attendee identity. |
| `VO-EventSchedule` | Start/end with time zone; end is not before start. |
| `VO-EventLocation` | Exactly one of physical address or Online. |
| `VO-Capacity` | Positive integer ceiling for a ticket type. |
| `VO-Slug` | URL-safe and unique among published events. |
| `VO-CoverImageRef` | Object key/reference only; never file bytes. |
| `VO-TicketCode` | Unique and unguessable QR payload. |
| `VO-ProviderReference` | External payment provider identifier. |
| Typed IDs | User, Event, TicketType, Reservation, Order, Payment, and Ticket identities MUST NOT be represented as interchangeable bare primitives in Domain. |

## 9. Events and lifecycle

### 9.1 Event catalogue

| Event | Scope | Primary consumers |
|---|---|---|
| `EVT-UserRegistered` | domain | local identity handlers |
| `EVT-EventPublished` | integration | reporting/discovery projections |
| `EVT-InventoryReserved` | domain | local workflow |
| `EVT-ReservationReleased` | integration | sales expiry workflow |
| `EVT-ReservationCommitted` | domain | local workflow |
| `EVT-EventSoldOut` | integration | realtime/reporting |
| `EVT-OrderPlaced` | domain | local workflow |
| `EVT-OrderConfirmed` | integration | ticketing, event management, reporting |
| `EVT-OrderExpired` | domain | local workflow |
| `EVT-PaymentInitiated` | domain | local workflow |
| `EVT-PaymentCaptured` | integration | sales |
| `EVT-PaymentFailed` | integration | sales |
| `EVT-PaymentRefunded` | integration | sales, ticketing |
| `EVT-TicketIssued` | integration | notifications, reporting |
| `EVT-TicketCheckedIn` | integration | realtime, reporting |
| `EVT-TicketTransferred` | integration | notifications, reporting |
| `EVT-TicketReturned` | integration | event management, payments |
| `EVT-EventCancelled` | integration | sales, payments, ticketing, notifications |

### 9.2 State transitions

**Event:** `Draft → Published → Closed`; `Published|Closed → Cancelled`.

**Order:** `Pending → Confirmed|Expired|Cancelled`; `Confirmed → Cancelled|Refunded` where the feature permits it.

**Payment:** `Initiated → Captured|Failed`; `Captured → Refunded`.

**Ticket:** `Valid → CheckedIn|Transferred|Void`; transfer creates a new Valid ticket.

Invalid transitions MUST return domain errors and MUST NOT partially mutate state.

## 10. Consistency, transactions, and messaging

### 10.1 Strong consistency

The no-oversell invariant lives in `AGG-Event`. `Reserve`, `ReleaseReservation`, `CommitReservation`, and `ReturnToPool` execute inside a database transaction and are guarded by optimistic concurrency. When two buyers race for the final unit, exactly one transaction may commit successfully.

At the intended scale, `AGG-Event` may be a hot aggregate. This is an accepted trade-off. Splitting inventory into a dedicated aggregate or service is out of scope until measured contention justifies it.

### 10.2 Order placement boundary

Creating an order and its inventory reservation is one application operation. In the modular monolith and shared database, it MAY update `AGG-Event` and create `AGG-Order` in one transaction to satisfy F-3.4/F-5.3. This is the sole deliberate exception to the default one-aggregate-per-transaction guideline.

### 10.3 Eventual consistency

Other cross-context side effects are asynchronous. Integration events MUST be persisted transactionally with the state change that produced them, then published after commit. Consumers MUST use stable message identifiers and an inbox/deduplication mechanism so redelivery is safe.

### 10.4 Required idempotency

- Provider webhooks are deduplicated by provider event/reference plus operation.
- `EVT-OrderConfirmed` may issue each logical ticket only once.
- Check-in retries return the prior successful result without admitting twice.
- Refund and cancellation consumers tolerate duplicate delivery.
- Notification consumers suppress duplicate logical sends.

### 10.5 End-to-end purchase flow

1. Attendee selects ticket types and quantities.
2. Application loads `AGG-Event`, reserves inventory, and creates Pending `AGG-Order` with price snapshots in one transaction.
3. A zero-total order confirms immediately; a paid order creates/initiates `AGG-Payment` through `IPaymentGateway`.
4. The provider callback is signature-verified and idempotently captures payment.
5. Sales confirms the order and emits `EVT-OrderConfirmed`.
6. Event Management commits the reservation; Ticketing issues one ticket per purchased unit.
7. Notifications sends ticket access asynchronously; Reporting updates projections.
8. At the door, Ticketing validates and checks in each code exactly once.

Hold expiry releases inventory and expires the order. Event cancellation fans out to sales cancellation, refunds, ticket voiding, and attendee notification.

## 11. Persistence and data ownership

- PostgreSQL, schema `app`, is authoritative.
- EF Core configurations and repositories live in Infrastructure.
- Mutable aggregates use an optimistic concurrency token.
- EF queries default to no tracking unless mutation or identity resolution requires tracking.
- Database migrations are append-only once shared; destructive edits require a new migration and an explicit data-migration plan.
- Redis is cache only; loss of Redis MUST NOT lose authoritative data.
- MinIO stores binary objects; relational records store object keys and metadata, not bytes or expiring presigned URLs.
- Read models MAY use dedicated tables but remain derived and rebuildable.
- Secrets and payment credentials MUST NOT be stored in source control or domain entities.

## 12. Runtime components and ports

| Component | Technology | Responsibility | Boundary |
|---|---|---|---|
| Relational store | PostgreSQL | authoritative aggregate and transactional data | repository/unit-of-work adapters |
| Cache | Redis | session/response cache, rebuildable data, optional SignalR backplane | cache/session ports |
| Object storage | MinIO | cover images and other binary assets | storage port |
| Messaging | RabbitMQ | integration events and asynchronous work | publisher/consumer adapters |
| Realtime | SignalR | server-to-client updates | Api hubs |
| Payment | trusted external provider | payment capture/refund | `IPaymentGateway` ACL |
| Email | external/local provider | ticket and organizer email | `IEmailSender` |
| Telemetry | OpenTelemetry + Seq | logs, traces, and metrics | `ServiceDefaults` |
| Orchestration | .NET Aspire | local topology, resource provisioning, service discovery | `AppHost` |

AppHost is the local topology source of truth. Service projects use native SDKs; they SHOULD NOT depend on Aspire client abstractions when ordinary vendor SDKs satisfy the port.

## 13. API and security conventions

- Minimal endpoints implement `IEndpoint` and are discovered by assembly scan.
- HTTP errors use RFC 7807 problem details.
- Malformed JSON/binding errors return 400; semantic validation errors return 422.
- Browser authentication uses secure, HttpOnly, SameSite-appropriate cookies and server-side session validation.
- Application handlers access identity through `ICurrentUserAccessor` and MUST perform event-scoped authorization for protected operations.
- Public event viewing and guest checkout remain unauthenticated where required by features.
- Payment webhooks MUST validate provider signatures before parsing trusted outcome data.
- Ticket access links and codes MUST be unguessable and scoped to the minimum data needed.
- Logs MUST NOT contain passwords, session secrets, raw payment payload secrets, or full ticket codes.
- SignalR clients authenticate using the same session model as REST.

## 14. Configuration and secrets

Configuration precedence is:

`appsettings.json → appsettings.Development.json → Aspire-injected environment → user secrets/environment secrets`.

Expected sections include `Session`, `Concurrency`, `Cache`, `Storage`, `Messaging`, `Realtime`, `Payment`, and `Logging`. Connection names follow AppHost resource names. Secrets MUST never be committed.

## 15. Observability and operations

- `ServiceDefaults` configures structured logging, OpenTelemetry traces/metrics, service discovery, and health checks once.
- OTLP exports to Seq using the AppHost-provided endpoint.
- Correlation and trace IDs flow through HTTP, MediatR, message publication, and consumers.
- `/health` exposes health checks and Aspire surfaces topology and resource health.
- Operational dashboards include Aspire, Seq, MinIO console, and RabbitMQ management.
- Integration-event failures MUST be observable with retry/dead-letter context rather than silently discarded.

## 16. Local development

1. Start Docker Desktop or a compatible container runtime.
2. Run `dotnet run --project src/AppHost/EventHub.AppHost.csproj`.
3. Aspire provisions PostgreSQL, Redis, MinIO, RabbitMQ, and Seq and injects connection data.
4. Use the Aspire dashboard for topology/health, Seq for logs/traces, MinIO for objects, and RabbitMQ UI for queues.
5. Development API documentation is exposed through Scalar at `/scalar`.

## 17. Verification strategy

### 17.1 Test levels

| Level | Required focus |
|---|---|
| Domain unit | aggregate behavior, value-object validation, state transitions, every high-risk `INV-*` rule |
| Application unit/component | handler orchestration, validation, authorization, idempotency decisions using port fakes |
| API integration | HTTP contracts, auth, EF mappings, PostgreSQL transactions/concurrency, and infrastructure boundaries |
| Consumer integration | RabbitMQ delivery, inbox deduplication, retry, and projection/notification behavior |
| End-to-end | only critical user journeys whose risk cannot be covered more cheaply, especially purchase → ticket → check-in |

Integration adapters SHOULD be tested against real engines through Testcontainers when an in-process fake would hide material behavior.

### 17.2 Mandatory risk scenarios

- concurrent purchase of the last available ticket: exactly one succeeds;
- hold expiry releases inventory and cannot later confirm;
- duplicated payment callback captures and confirms at most once;
- duplicated `EVT-OrderConfirmed` does not duplicate tickets;
- repeated ticket scan admits once and returns a stable duplicate-scan response;
- event cancellation is safe under message redelivery;
- price changes after placement do not alter an existing order;
- a Staff user cannot perform Owner-only operations.

### 17.3 Feature completion

A feature may be marked `implemented` in `features.md` only when its acceptance criteria are represented in code and the highest-risk criteria have automated evidence. “Implemented” is not equivalent to production-ready; unresolved operational limitations MUST be documented here.

## 18. OpenAPI contract

REST shapes are maintained in `contracts/openapi/api.v1.yaml`. Build tooling exports or verifies the runtime API against that contract. `web/src/generated/` is generated output and MUST NOT be hand-edited.

## 19. Feature and domain traceability

| Epic | Primary bounded context(s) | Main technical anchors |
|---|---|---|
| EP-1 | BC-1 | authentication, session adapter, event-scoped RBAC |
| EP-2 | BC-2 | event lifecycle, image storage, authorization |
| EP-3 | BC-2/BC-3 | inventory invariant, money, pricing snapshots, concurrency |
| EP-4 | BC-2/BC-7 | public read models and HTTP queries |
| EP-5 | BC-2/BC-3 | reservation + order transaction, expiry workflow |
| EP-6 | BC-4/BC-3 | payment ACL, webhook idempotency, refund workflow |
| EP-7 | BC-5/BC-6 | ticket factory, access links, asynchronous email |
| EP-8 | BC-5 | code validation, exactly-once admission semantics |
| EP-9 | BC-7/BC-6 | projections, exports, results, messaging |
| EP-10 | BC-5/BC-2/BC-4 | transfer invariants, inventory return, refund |
| EP-11 | BC-2/BC-3/BC-5 | integration events and SignalR projections |

## 20. Maintenance rule

This is one of exactly three authoritative EventHub specifications. Separate domain-model, architecture-MOC, database-design, or technical-design documents **MUST NOT** become competing sources of truth. Focused ADRs or migration plans MAY exist as implementation evidence, but durable architecture and domain decisions must be reconciled here.
