---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: eventhub.spec.pending-feature-completion
title: Pending Feature Completion
status: implemented
last_updated: 2026-07-13
owner: builder
language: en
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
applies_to:
  - F-6.6
  - F-8.5
  - F-8.6
  - F-10.3
  - F-10.4
  - F-11.2
  - F-11.3
---

# Pending Feature Completion

## Problem

`docs/pending-features.md` identified remaining planned features without dedicated implementation evidence: refund on cancellation, multi-device/offline check-in tolerance, return-to-pool, live check-in progress, and sold-out/low-stock nudges. Existing source already implemented the broader platform spine, but these final planned items needed explicit behavior, code, tests, and status reconciliation.

## Scope

In scope:

- F-6.6: cancelling an event refunds captured paid orders and invalidates issued tickets.
- F-8.5: concurrent or repeated check-in attempts preserve exactly-once admission.
- F-8.6: offline scan queues reconcile through the existing authenticated batch-sync endpoint, with a durable replay result for each `(event, clientScanId)` operation when connectivity returns.
- F-10.3/F-10.4: a holder can return an eligible ticket to a sold-out ticket type before event start; the ticket is voided, inventory returns, and payment is refunded when present.
- F-11.2: authorized check-in clients can receive live door-count updates over SignalR.
- F-11.3: live sales/inventory payloads identify sold-out and low-stock ticket types for owner/staff nudges.

Out of scope:

- A production payment provider integration beyond the existing local provider boundary.
- A full offline-first browser/PWA scanner UI.
- Custom organizer-configurable low-stock thresholds.

## Business Rules

- Refunds are idempotent at the payment aggregate: already-refunded payments are not refunded twice.
- Event cancellation voids valid issued tickets so cancelled-event tickets cannot be admitted.
- Batch check-in sync processes up to 100 queued scans. Its durable cross-request replay semantics, including duplicate identifiers and duplicate ticket codes, are defined in the F-8.6 slice below.
- Return-to-pool is allowed only while the ticket type is sold out and before event start.
- Low stock is true when remaining inventory is greater than zero and at or below `min(3, 20% of capacity)`, with a minimum threshold of one.
- SignalR hub joins reuse the same session and event-scoped permission model as REST.

## Acceptance Criteria

- GIVEN an event with captured paid orders, WHEN the owner cancels the event, THEN captured payments are refunded, affected orders are marked refunded, and issued tickets are voided.
- GIVEN an authenticated check-in client replays a queued scan for the same event and client scan identifier, WHEN its validated code and scan instant match the original request, THEN the server returns the durable original result without admitting twice; a changed payload is rejected and cannot overwrite that result.
- GIVEN duplicate scans arrive from multiple devices or queued offline batches under different client scan identifiers, WHEN the server reconciles them, THEN only one scan admits the ticket and every later identifier receives a durable rejected result with a stable reason.
- GIVEN a sold-out ticket type before event start, WHEN the holder returns a valid ticket, THEN the ticket is voided, the order is marked refunded, and one unit returns to availability.
- GIVEN a ticket type is not sold out or event start has passed, WHEN a return is requested, THEN the return is refused with a clear reason.
- GIVEN an authorized check-in user has joined the event check-in hub group, WHEN a ticket is checked in, THEN the updated checked-in and issued counts are broadcast.
- GIVEN live sales inventory changes, WHEN organizer/staff clients receive the realtime payload, THEN each ticket type includes sold-out and low-stock flags.

## F-8.6 Durable Offline Replay Repair

### Current implementation drift

`BatchCheckInTicketsCommandHandler` currently accepts `ClientScanId` and `ScannedAt`, but uses
the identifier only to echo each response and does not use the supplied scan instant. Its
`acceptedCodes` set exists only for the current command invocation and is populated only after an
accepted result. A retry submitted in a later HTTP request therefore re-runs ticket lookup and
check-in rather than returning a durable original operation result. The existing in-batch
duplicate-code rejection is useful as a local guard, but it is not a persisted idempotency key and
does not distinguish an identical retry from a caller reusing an identifier for changed input.

The existing event-scoped endpoint and `BatchCheckInTicketsCommand` already require authentication
and the Check-in permission. This repair preserves that boundary; it does not make an offline
queue or ticket-admission operation public.

### Bounded scope

In scope for this repair only:

- Preserve the existing `POST /api/events/{eventId}/check-ins/sync` route and batch limit of 100.
- Treat `(EventId, ClientScanId)` as the durable identity of one queued scan operation.
- Persist a fingerprint of the canonical request identity and the first committed result, whether that result is an
  acceptance or a business-rule rejection.
- Replay the stored result only when the incoming canonical code and scan instant match the stored
  identity; reject a changed payload without mutating a ticket or replacing the stored result.
- Preserve the existing ticket-level exactly-once rule when two distinct operation identifiers
  target the same ticket.
- Keep the actual admission time server-authoritative.

Out of scope for this repair:

- A browser/PWA offline queue, service worker, scanner-camera UX, background sync, or retry UI.
- Changing the event Check-in permission model, session behavior, or anonymous access rules.
- A new public sync route, generated-client hand edits, realtime behaviour, reporting, or
  client-configurable retention policy.
- Reusing a client scan identifier across events as a global identifier; event scoping is deliberate.

### Replay identity and observable behaviour

For each syntactically valid queued item, derive its replay identity before ticket mutation:

- `EventId` is the route event identifier after the normal authorization pipeline has permitted
  the operation.
- `ClientScanId` is the existing non-empty, maximum-100-character client value. Scanner queues
  must generate it uniquely per event operation; a collision is protected by payload matching,
  not silently accepted.
- The canonical code is the existing `TicketCode` value after its normal trim and validation. It is
  case-preserving; this repair must not introduce a different code comparison rule from ticket
  lookup or code uniqueness. Persist only its deterministic lowercase SHA-256 hexadecimal
  fingerprint (64 characters); a replay record must never retain a second raw ticket-code copy.
- The canonical scan instant is `ScannedAt` normalized to UTC and PostgreSQL's microsecond storage
  precision before comparison, not as the serialized offset text. For example, two offset
  representations of the same instant are the same replay payload, and a retry cannot mismatch
  merely because its source carries sub-microsecond ticks.

The first committed operation stores its canonical identity and result. A later request with the
same event, identifier, canonical code, and canonical instant returns that stored result without
executing `Ticket.CheckIn` again. This includes a stored rejection: an operation that was rejected
because its order or ticket state was invalid is not re-evaluated later under the same identifier.
The scanner must create a new identifier for a genuinely new scan.

If an existing `(EventId, ClientScanId)` has a different canonical code or instant, return a
stable rejected item result for the replay mismatch. Do not load or mutate the newly supplied
ticket, do not overwrite the original record, and do not convert the mismatch into a replay. A
malformed code or other request-level validation failure retains the existing rejection behaviour
and must not mutate admission state.

`ScannedAt` is client-provided evidence for replay identity only. The first successful admission
continues to call `Ticket.CheckIn` with `IClock.UtcNow`; its `CheckedInAt` and the accepted replay
response therefore record the server's authoritative processing time, never a delayed or forged
client timestamp.

### Data and transaction boundary

Add an application-owned persistence record for a resolved batch item, for example
`CheckInReplayRecord`. It is not a new ticket aggregate and must not duplicate or replace the
ticket's lifecycle state. Its minimum durable data is:

| Data | Purpose |
| --- | --- |
| `EventId`, `ClientScanId` | Composite replay key with a database unique constraint. |
| SHA-256 fingerprint of canonical code, canonical `ScannedAtUtc` | Immutable payload identity used to decide replay versus mismatch without retaining a raw ticket code. |
| `Accepted`, response status, stable reason when rejected | Durable result for accepted and rejected operations. |
| Optional `TicketId` and server `CheckedInAt` | Reconstruct the original accepted response without using client time or storing a second ticket lifecycle. |
| Resolution timestamp / normal persistence metadata | Operational traceability; it must contain no credentials or raw request logging. |

The unique key is exactly `(event_id, client_scan_id)`. It must not be global: the same identifier
may be used independently in two events and must create/return two independent results. The record
and any ticket state mutation must commit in the same PostgreSQL unit-of-work transaction. A
response must never claim a newly resolved accepted or rejected operation whose replay record did
not commit.

The model snapshot also retains the application-managed `ValueGeneratedOnAdd` row-version metadata
for `DiscountCode`, `Order`, `Payment`, and `Ticket`. That metadata has no schema operation; the
generated replay migration's `Up` contains only the replay table, indexes, and reviewed foreign
keys.

The record may reference a ticket only for a result that safely exposes the existing authorized
check-in projection. It must not persist a second copy of holder data merely for replay. Existing
ticket data remains the source for the projection, while the stored server check-in time preserves
the original admission result.

### Edge cases

- **Same identifier, same canonical payload, later request:** return the first committed result,
  including its original server check-in time; do not emit another ticket-checked-in event.
- **Same identifier, different code or different instant:** reject as a replay mismatch. The first
  record and any ticket it resolved remain unchanged.
- **Equivalent timestamp offsets:** treat equal UTC instants as the same payload, not a mismatch.
- **Same identifier in different events:** keep independent replay records and outcomes. Each
  request remains independently authorized for its route event.
- **Same ticket, different identifiers:** process each as a distinct operation. Exactly one may
  accept; the later operation is stored as a rejection such as the existing already-checked-in
  outcome, and its own replay returns that rejection.
- **Duplicate identifiers within one submitted batch:** apply the same replay/mismatch rules as
  separate requests, preserving input order in the returned results and creating at most one record
  for the key.
- **Unknown, wrong-event, unconfirmed, void, transferred, or already-checked-in ticket:** store
  the resolved business-rule rejection for a syntactically valid operation so its retry remains
  deterministic. It must not affect another event's ticket or a later operation with a different
  identifier.
- **Transaction failure before commit:** leave neither a replay record nor a ticket state change;
  the client may retry the operation.

### Concurrency and failure handling

- The database unique constraint is the final authority for two concurrent deliveries of the same
  replay key. A losing request must reload and return the winner's stored result when the canonical
  payload matches; it must not surface a unique-key failure or re-run admission.
- Distinct replay keys for the same ticket continue through the Ticket aggregate's optimistic
  concurrency boundary. On a ticket conflict, retry from authoritative PostgreSQL state so the
  losing identifier records the final rejection rather than admitting twice.
- The replay record is not a substitute for ticket row-version protection. It coordinates one
  client operation; `INV-40` still protects all admission paths, including direct scans and manual
  check-in.
- A replay record must be written only after authorization for the target event. An unauthenticated
  or unauthorized request returns the existing 401/403 result and creates no replay state.
- Pending domain and realtime events from a failed concurrency attempt must be discarded by the
  normal unit-of-work retry boundary so a replay or losing scan cannot publish duplicate admission
  side effects.

### Area obligations

| Area | Required work | Explicit boundary |
| --- | --- | --- |
| Domain | Preserve `Ticket.CheckIn`, `INV-40`, and the server-owned check-in timestamp. | No replay persistence or transport concerns in `EventHub.Domain`. |
| Application | Introduce a replay persistence port/model, canonicalize each batch item, resolve/replay/mismatch before ticket mutation, and retain `IAuthorizeEventOperation` Check-in authorization. | Do not trust `ScannedAt` as the admission time or bypass the normal command/unit-of-work pipeline. |
| Infrastructure | Add the EF record/configuration/repository and an additive migration with the composite unique index; make same-key races reload the durable result. | Do not alter existing migrations or use cache/in-memory state as the replay authority. |
| API and Contracts | Keep the authenticated event-scoped route and existing request/response shape. Represent a mismatch as an existing rejected batch item with a stable clear reason. | No new anonymous endpoint; update OpenAPI/generated outputs only if the public shape is deliberately changed, which this slice does not require. |
| Web and E2E | No UI or PWA work is required for the bounded server repair. Existing queues must continue sending their stable client scan identifier, code, and scan instant. | Do not hand-edit generated client output or claim offline browser resilience beyond server reconciliation. |
| Tests | Add focused real-PostgreSQL API integration coverage for durable replay and races. | Unit-only fakes are insufficient for the unique-key and optimistic-concurrency guarantees. |

### Testing and verification strategy

- Replay an accepted item in a second HTTP request with the same event, identifier, trimmed/canonical
  code, and equal UTC scan instant; assert one checked-in ticket, one replay row containing only a
  code fingerprint, an unchanged
  server check-in time, and no duplicate event-side effect.
- Repeat an item using the same identifier with a changed code, then with a changed scan instant;
  assert a rejected mismatch each time, the original replay row remains intact, and neither changed
  payload can mutate a ticket.
- Submit equal scan instants expressed with different offsets, including an instant with sub-microsecond
  ticks, and verify they replay the same result.
- Reuse an identifier in two independently authorized events and verify both operations remain
  independent, including separate replay rows and ticket outcomes.
- Concurrently submit two distinct identifiers for the same valid ticket through separate clients;
  assert exactly one accepted admission, one durable already-checked-in-style rejection, and stable
  replay of each identifier's own outcome.
- Verify a historical or future client `ScannedAt` never becomes `Ticket.CheckedInAt`; the accepted
  response and durable replay return the fixed server clock time.
- Verify anonymous and event-unauthorized sync requests remain 401/403 and create no replay row.
- Retain the current within-batch duplicate-code and batch-size tests, then run the focused API
  integration tests, the full API integration project, the .NET suite, and the changed-code
  verifier. Browser E2E is excluded unless a later UI queue change is separately scoped.

## Verification

- `dotnet test tests/Api.IntegrationTests/EventHub.Api.IntegrationTests.csproj -c Release`
- `yarn --cwd web api:verify`
- `yarn --cwd web build`
