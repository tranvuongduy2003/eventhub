---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260712125104-live-sales-inventory
title: Live Sales and Inventory
status: proposed
last_updated: 2026-07-12
owner: builder
language: en
applies_to:
  - F-11.1
  - EP-11
  - Reporting & Audience
  - Event Monitoring Hub
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# Live Sales and Inventory

## Problem

EventHub already records ticket-type inventory, pending reservations, confirmed orders, issued tickets, and event results. Organizers can open the event results view to see sales by ticket type and revenue, but that view is a static REST snapshot. During active sales an organizer must refresh manually to know whether tickets have sold or remaining availability has changed.

F-11.1 exists to add immediacy to the organizer event results view only. It must not change the no-oversell model, the purchase flow, the reporting authorization boundary, or the static REST reporting surface's role as the recoverable source for missed updates.

## Solution

Complete the existing event monitoring hub as an event-scoped SignalR stream for live sales and inventory snapshots. A signed-in Owner or Staff user with `Permission.Reporting` joins the sales-inventory stream for a single event from the event results page. When a committed inventory or sales change occurs, the server broadcasts a full sales-and-inventory snapshot for that event to the authorized event group. The frontend applies the snapshot to the existing event results query cache so sold counts and remaining availability update without manual refresh.

The realtime message is a notification over authoritative PostgreSQL state. Every payload must be rebuildable from existing event, ticket-type, order, and order-line data. If a message is missed, reconnecting or refreshing the REST results must restore correct state.

This spec does not approve adding `@microsoft/signalr` or any other production frontend dependency. The frontend implementation must either use an already-approved dependency or a small local browser client for the SignalR JSON protocol. Any request to add `@microsoft/signalr` needs explicit approval and license review before implementation.

## Source Alignment

- `docs/product.md` anchors the slice in `G-5` organizer clarity, `QG-1` simplicity, `QG-4` mobile-friendly views, and `QG-5` correctness with no oversell.
- `docs/features.md` defines F-11.1 as a Next-phase feature: users with Reporting permission see sold count and remaining availability update without manual refresh; users without Reporting permission do not receive live sales updates.
- `docs/technical.md` requires PostgreSQL as authoritative (`ARCH-6`), SignalR only for server-push enhancements, Api ownership of hubs (`ARCH-4`), idempotent retriable boundaries (`ARCH-8`), and same-session authentication for SignalR clients.
- `docs/specs/20260625161104-inventory-and-no-oversell.md` keeps inventory correctness in `AGG-Event` and defines capacity, sold, reserved, and available as the source counters.
- `docs/specs/20260704203404-create-order-hold-inventory.md` defines Pending orders and reservations; F-11.1 reads those counters but does not change order placement.
- `docs/specs/20260710204440-audience-results.md` defines the static event results surface and Reporting permission boundary that this slice enhances.
- Current source already has `EventMonitoringHub` mapped at `/hubs/events`, but the hub is empty and does not yet enforce event-scoped join authorization.
- Current source has `GetEventResultsQuery`, `IReportingRepository.GetEventResultsAsync`, `EventResultsResponse`, and `event-results-page.tsx`; these expose sold and revenue but not capacity, reserved, or remaining counts.
- Current source has domain events for `InventoryReservedEvent`, `ReservationCommittedEvent`, `ReservationReleasedEvent`, `InventoryReturnedToPoolEvent`, and `EventSoldOutEvent`. Broadcasts must be triggered only after the transaction that produced those events commits.
- Source-spec drift to reconcile later: `technical.md` section 10.3 says the current implementation has no async messaging infrastructure, while source contains Channel integration-event queue/publisher/consumer types. This spec may use the current Channel surface only as a narrow post-commit delivery mechanism; it must not expand or redefine the durable messaging architecture.

## In Scope

- F-11.1 only: live organizer sales and inventory updates on the event results view.
- Completing the existing `/hubs/events` SignalR hub with authenticated, event-scoped join and leave methods for sales-inventory monitoring.
- Backend event-scoped authorization for hub joins using the same session identity and `Permission.Reporting` semantics as `GetEventResultsQuery`.
- A full sales-and-inventory snapshot payload containing event totals plus per-ticket-type capacity, sold, reserved, remaining, and revenue fields.
- Extending the static event results contract enough for the event results page to render and refetch the same inventory fields used by the realtime snapshot.
- Post-commit broadcast triggers for committed inventory changes that affect sold, reserved, or remaining counts.
- Frontend subscription lifecycle on the existing event results page: connect while mounted, join the current event, update TanStack Query cache on snapshot receipt, leave or dispose on unmount, and refetch after reconnect.
- Focused API/integration tests, application/repository tests, frontend build/type checks, and browser/manual verification where useful.

## Out of Scope

- F-11.2 live check-in progress, door counts, or cross-device check-in synchronization.
- F-11.3 sold-out and low-stock nudges, thresholds, owner-only alerts, notification UI, or toast/banner behavior.
- Public attendee event-page live availability.
- Attendee-facing checkout reservation progress.
- New authoritative reporting tables, Redis-only state, distributed inventory services, or changes to no-oversell invariants.
- Adding `@microsoft/signalr` or another production frontend dependency without explicit approval and license review.
- Reworking the Channel integration-event architecture beyond the narrow post-commit trigger needed for this feature.
- Multiple occurrence realtime behavior from F-2.7; if occurrences later become active in sales, they need a separate extension spec.

## Personas and Permissions

- `PER-O1` individual organizer: can watch sold and remaining availability move on their event results view.
- `PER-O2` small group organizer or assigned staff member: can monitor sales for events where they have Reporting permission.
- Owner role satisfies Reporting permission.
- Staff may receive live sales and inventory updates only when their event role includes Reporting permission.
- Unauthenticated users, attendees, unrelated organizers, staff without Reporting permission, and users authorized for a different event must not join the group or receive payloads.
- Frontend route guards are UX only; the hub and any snapshot query must enforce authorization on the backend.

## Dependencies

- F-1.7 event-scoped authorization for Reporting permission.
- F-3.4 inventory counters and no-oversell behavior on ticket types.
- F-5.3 Pending orders and inventory holds.
- F-5.5 hold expiry and reservation release for remaining-availability recovery.
- F-6.2/F-6.3 free and paid order confirmation, which moves reserved inventory to sold.
- F-9.3 static event results view and reporting read model.
- Existing ASP.NET Core SignalR server support in `Api`; the web client dependency decision remains unresolved and must not be assumed.

## Acceptance Criteria

- `AC-01` GIVEN I am signed in and hold Owner or Staff with Reporting permission for Event A, WHEN I open Event A's results view, THEN the browser connects to `/hubs/events`, joins Event A's sales-inventory stream, and receives an initial or immediately refetched snapshot for Event A.
- `AC-02` GIVEN I am not signed in, WHEN I attempt to connect or join an event sales-inventory stream, THEN the server refuses the connection or join with an unauthorized outcome and sends no event data.
- `AC-03` GIVEN I am signed in but have no role on Event A or lack Reporting permission, WHEN I call the join method for Event A, THEN the server returns an insufficient-permissions outcome, does not add the connection to Event A's group, and sends no Event A sales updates.
- `AC-04` GIVEN I have Reporting permission for Event A but not Event B, WHEN Event B has sales or inventory activity, THEN my Event A connection receives no Event B payloads.
- `AC-05` GIVEN Event A's results view is open and authorized, WHEN a Pending paid order reserves inventory for Event A, THEN remaining availability and reserved count for affected ticket types update without manual refresh, while sold count and revenue do not increase yet.
- `AC-06` GIVEN Event A's results view is open and authorized, WHEN a paid order is confirmed and its reservation is committed, THEN sold count and revenue increase, reserved count decreases, and remaining availability remains consistent with `capacity - sold - reserved` without manual refresh.
- `AC-07` GIVEN Event A's results view is open and authorized, WHEN a zero-total order auto-confirms, THEN sold count increases, revenue remains unchanged for that free line, and remaining availability updates without manual refresh.
- `AC-08` GIVEN Event A has a Pending order whose hold expires, WHEN the reservation is released, THEN reserved count decreases and remaining availability increases without changing sold count.
- `AC-09` GIVEN Event A has multiple ticket types and an order affects more than one type, WHEN the update is broadcast, THEN the payload includes all ticket types for Event A and the UI updates each affected row atomically from one full snapshot.
- `AC-10` GIVEN a ticket type has zero confirmed sales, WHEN a live or REST snapshot is rendered, THEN that ticket type still appears with sold count `0`, revenue `0`, and remaining availability based on capacity and reserved count.
- `AC-11` GIVEN two purchases complete close together, WHEN multiple broadcasts are received, THEN each payload is a full snapshot and applying the latest event snapshot keeps the UI consistent with a REST refetch from PostgreSQL.
- `AC-12` GIVEN the realtime connection drops and later reconnects, WHEN the event results page resumes, THEN it refetches the REST results snapshot before trusting later live updates and rejoins only the current event's stream.
- `AC-13` GIVEN the event results page unmounts or the user navigates to another event, WHEN cleanup runs, THEN the browser leaves the old event stream or stops the connection so the old page no longer receives updates.
- `AC-14` GIVEN realtime transport is unavailable or blocked, WHEN the user opens the event results page, THEN the static REST results still render and the UI does not display misleading stale-live status as authoritative.
- `AC-15` GIVEN a realtime payload is logged, traced, or surfaced in errors, THEN it contains only aggregate counts, money totals, identifiers needed for the view, and correlation metadata; it does not contain attendee names, emails, payment provider references, session IDs, cookies, tokens, or full ticket codes.

## Business Rules and Invariants

- PostgreSQL remains authoritative. Realtime payloads are derived snapshots, not source data.
- F-3.4 and `INV-10` remain unchanged: `Reserved + Sold <= Capacity` for every ticket type.
- Remaining availability is always `capacity - sold - reserved` and must never be negative.
- Sold count represents confirmed sales only. Pending reservations reduce remaining availability through `reservedCount` but do not increase sold count or revenue.
- Revenue is gross confirmed order-line revenue in the configured currency because EventHub has no platform fee.
- A broadcast must occur only after the database transaction commits. Do not send hub messages from pre-commit domain-event handlers.
- A subscription is event-scoped. Joining one event does not grant access to any other event.
- Reporting permission is checked at join time and again after reconnect. Permission-change push while a connection is already joined is out of scope.
- Snapshot delivery is at-least-once from the application's perspective. Clients must tolerate duplicate snapshots.
- Missed messages are acceptable because REST results can reconstruct state; stale or partial client state must not be treated as authoritative.
- No ticket issuance, check-in, payment capture, inventory mutation, or order lifecycle behavior changes are part of this feature.

## Domain/Application Design Notes

- Do not add SignalR references to `Domain` or `Application`. SignalR stays in `Api` per `ARCH-4`.
- Add application-level result types for the live sales-inventory snapshot, for example `LiveSalesInventorySnapshotResult` and `TicketTypeSalesInventoryResult`.
- Reuse the Reporting permission model by introducing a query such as `GetLiveSalesInventorySnapshotQuery : IQuery<LiveSalesInventorySnapshotResult>, IAuthorizeEventOperation` with `RequiredPermission => Permission.Reporting`.
- The static `GetEventResultsQuery` may be extended to return the same per-ticket-type inventory fields, or it may share an internal projection method with the live snapshot query. Either way, the REST and realtime calculations must agree.
- Api should complete `EventMonitoringHub` as a thin transport surface: authorize/join/leave groups and send typed client events. Business rules and snapshot calculation stay in Application/Infrastructure.
- Use a server-only group name such as `event:{eventId}:sales-inventory`. Clients pass only `eventId`; never accept a raw group name from the browser.
- Broadcast triggers should collect affected `EventId` values from committed inventory events and publish one full snapshot per affected event. Coalescing multiple events for the same event in one request is allowed.
- If the current MediatR domain-event pipeline dispatches before commit, add a narrow post-commit notification path before broadcasting. A post-commit collector/behavior or Channel consumer is acceptable if it preserves Clean Architecture boundaries and idempotency.
- Avoid putting full snapshot construction in hub methods if that would duplicate reporting query logic; the hub should call the same Application query used by REST/refetch.

## API and Contract Expectations

### Hub

- Path: `/hubs/events` (already mapped in current source).
- Authentication: existing `Session` cookie authentication; the hub endpoint or hub class must require authorization.
- Join method: `JoinEventSalesInventory(int eventId)`.
- Leave method: `LeaveEventSalesInventory(int eventId)`.
- Client event: `eventSalesInventoryUpdated`.
- Unauthorized connection or join failures must use SignalR errors/statuses that do not disclose whether the event exists beyond the normal REST authorization behavior.

### Snapshot Payload

The live payload is not an OpenAPI REST path, but it should be represented by typed C# and TypeScript contracts to keep the frontend and tests stable.

```json
{
  "eventId": 42,
  "eventTitle": "Workshop",
  "inventoryVersion": 17,
  "issuedCount": 8,
  "totalRevenueAmount": 250000,
  "totalRevenueCurrency": "VND",
  "ticketTypes": [
    {
      "ticketTypeId": 7,
      "ticketTypeName": "General",
      "capacity": 20,
      "soldCount": 8,
      "reservedCount": 1,
      "remainingCount": 11,
      "revenueAmount": 250000,
      "revenueCurrency": "VND"
    }
  ],
  "occurredAt": "2026-07-12T05:51:04Z"
}
```

Contract rules:

- `eventId`, `ticketTypeId`, and `ticketTypeName` identify rows already visible in the authorized results view.
- `inventoryVersion` should use the event row-version or another monotonic per-event version available from authoritative persistence. If no safe monotonic value is available, omit it and rely on full snapshot refetch after reconnect; do not invent a client-only sequence as authoritative.
- `issuedCount` and revenue fields must match the static event results semantics.
- `capacity`, `soldCount`, `reservedCount`, and `remainingCount` come from persisted ticket-type inventory counters.
- All money fields use decimal-compatible JSON numbers and a currency code matching existing response conventions.
- `occurredAt` is the server timestamp for snapshot creation, not proof of transaction time.

### REST Contract

The existing `GET /api/events/{eventId}/results` endpoint must remain the recovery snapshot for the event results page. Extend `EventResultsResponse` and `TicketTypeSalesResponse` or their replacements to include the per-ticket-type inventory fields needed by F-11.1:

- `capacity`
- `reservedCount`
- `remainingCount`
- optionally `inventoryVersion` if used by the live payload

Because this is a REST contract change, implementation must update `contracts/openapi/api.v1.yaml`, regenerate frontend types through the existing contract workflow, and avoid hand-editing `web/src/generated/`.

## Data and Persistence Expectations

- No database migration is expected for F-11.1 if current ticket-type `capacity`, `sold`, `reserved`, and event `row_version` records are sufficient.
- Snapshot source data:
  - Event title and optional event row version from `Events`.
  - Capacity, sold, reserved, and available from `TicketTypes`.
  - Revenue from confirmed `Orders` and `OrderLines`.
  - Issued count from issued `Tickets`, preserving current event results semantics.
- Query all ticket types for the event, including those with no confirmed order lines.
- Use no-tracking EF queries for snapshot reads.
- Do not persist realtime connection state in PostgreSQL.
- Redis backplane remains optional configuration. A single API host must satisfy this feature without Redis-backed SignalR scale-out.
- Read models and caches remain rebuildable from PostgreSQL.

## Frontend Expectations

- The existing event results page remains the target UI; do not add a new dashboard route for F-11.1.
- Static REST results render first and remain useful if realtime fails.
- When results load for a valid `eventId`, the page opens at most one active sales-inventory subscription for that event.
- On `eventSalesInventoryUpdated`, update the TanStack Query cache for `['event-results', eventId]` or the current source-equivalent key so existing cards and ticket-type rows re-render.
- Show remaining availability in the ticket-type results table alongside sold and revenue. Include reserved count only if the design can present it as operational inventory without confusing it with sold tickets; otherwise it may remain internal to the calculation while still present in data.
- On reconnect, invalidate/refetch event results before applying later live snapshots.
- On unauthorized join or subscription failure, do not expose extra event details. Keep the REST authorization error as the visible access boundary.
- On navigation to a different event, leave the prior event group before joining the new one.
- Do not add `@microsoft/signalr` unless explicit approval and license review are obtained. If no approval exists, keep the local realtime helper small, isolated, and covered by tests around message parsing and cleanup.

## Integration and Messaging Expectations

- Broadcast on changes that affect F-11.1 data:
  - inventory reserved;
  - reservation committed;
  - reservation released;
  - inventory returned to pool if a future implemented flow invokes it;
  - ticket type capacity/name changes only if current rules allow such changes after the event is visible in results.
- Do not broadcast directly from domain entities or from pre-commit domain-event handlers.
- Prefer one post-commit event notification per affected event and send a full snapshot, not field-level deltas.
- If the current Channel integration-event queue is used, consumers must be idempotent and must fetch the latest snapshot from PostgreSQL at consume time.
- If no consumer is registered for an integration event, the feature must not silently appear complete; tests must prove the F-11.1 broadcast path is wired.

## Security, Privacy, and Abuse Considerations

- Require authenticated sessions for hub connections and joins.
- Validate Reporting permission server-side for every join.
- Do not trust `eventId` from the client beyond using it as an authorization input.
- Never accept client-provided group names.
- Target broadcasts only to the event sales-inventory group.
- Payloads must not include attendee names, emails, order contact details, payment references, role assignments, session IDs, cookies, tokens, or full ticket codes.
- Logs may include event ID, ticket type ID, connection ID hash or short correlation ID, and failure category. Logs must not include cookies, session IDs, connection tokens, full payloads with sensitive data, or credentials.
- Rate limiting is not required for this slice, but repeated unauthorized join attempts should be observable through structured logs.
- CORS and credentials behavior for development must preserve existing session-cookie controls.

## Observability and Audit

- Add structured logs for successful joins, refused joins, leaves/disconnect cleanup, broadcast attempts, and broadcast failures.
- Include correlation or trace IDs where available through `ServiceDefaults`.
- Metrics should count active event monitoring connections if practical, plus broadcast success/failure counts. Lack of metrics must not block the slice if tests and logs provide adequate evidence.
- No durable audit log entry is required for viewing live sales data; this is read access equivalent to opening the results page.
- Broadcast failures must be visible in logs and must not roll back the already-committed purchase or inventory transaction.

## Edge Cases and Failure Modes

- Event has no ticket types: snapshot returns an empty ticket-type list, zero sold, zero revenue, and zero issued count.
- Event has ticket types but no sales: each ticket type appears with capacity, zero sold, current reserved count, and remaining availability.
- Free ticket confirmation: sold increases and revenue remains zero for the free line.
- Pending paid order: reserved increases and remaining decreases; sold and revenue wait for confirmation.
- Hold expiry: reserved decreases and remaining increases.
- Multi-ticket-type order: all affected ticket types are included in one full event snapshot.
- Concurrent purchases: optimistic concurrency remains in the purchase/inventory flow; realtime sends resulting snapshots only after successful commits.
- Duplicate broadcast: client applies the same full snapshot safely without double-counting.
- Connection drops before receiving a broadcast: reconnect refetch restores state.
- Role revoked while already connected: the next join/reconnect is refused. Immediate server-initiated removal on permission changes is out of scope.
- Event cancelled or closed: no new purchases should occur through existing rules; any final inventory snapshot may be sent if a committed reservation release or cancellation-related inventory change occurs.
- Hub unavailable: static REST results still load; live behavior is absent rather than misleading.

## Testing and Verification Strategy

- API/integration test: authenticated Owner can connect to `/hubs/events`, join their event, and receive a sales-inventory snapshot after a committed reservation or order confirmation path.
- API/integration test: Staff with Reporting permission can join; Staff without Reporting permission, unrelated organizers, and unauthenticated users cannot join or receive payloads.
- API/integration test: a user authorized for Event A does not receive Event B updates.
- Application/repository test: snapshot includes every ticket type and computes capacity, sold, reserved, remaining, and revenue from PostgreSQL-backed data consistently with `GET /api/events/{eventId}/results`.
- Application/integration test: broadcasts are post-commit. A failed or rolled-back order/inventory operation must not produce a client-visible snapshot that claims the failed state.
- Frontend test or component-level verification: receiving `eventSalesInventoryUpdated` updates the event results query cache and ticket-type UI without a manual refresh.
- Frontend test or manual verification: reconnect invalidates/refetches the REST event results before applying later updates.
- Contract verification: OpenAPI source and generated frontend types are synchronized if REST result shapes change.
- Build verification: run the narrowest affected backend and frontend checks while implementing, then `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1` before handoff.
- Manual/browser verification when Aspire is available: open an authorized event results view, place a paid Pending order in another session, confirm it or use the local payment confirmation flow, and observe reserved/sold/remaining changes without refreshing.

## Risks and Follow-up Decisions

- Frontend SignalR client choice is unresolved. `@microsoft/signalr` is the official client, but this spec does not approve it as a production dependency.
- Post-commit broadcast wiring is the highest-risk backend detail because current domain-event handlers may run before transaction commit. Implementation must prove post-commit behavior with tests.
- REST event results currently lack inventory fields. F-11.1 needs those fields for recovery and UI consistency; this is expected implementation work, not source-spec drift.
- `technical.md` and current source differ on whether Channel integration-event infrastructure exists. A future source-spec reconciliation should either document the Channel surface or remove it from source if it is not intended.
- F-11.2 needs a separate spec after multiple-device check-in consistency is accepted.
- F-11.3 needs a separate spec after owner-only low-stock threshold and notification UX decisions are made.

## Implementation Planning Notes

- Start by extending the reporting snapshot/query so REST and realtime can share one calculation.
- Complete hub authorization before broadcasting; do not rely on frontend guards.
- Keep the hub method surface small: join, leave, and server-to-client snapshot event.
- Prefer typed hub client interfaces on the server if they reduce stringly-typed event names without leaking SignalR outside Api.
- Add post-commit event ID collection for inventory-affecting events, then broadcast by loading the latest snapshot from PostgreSQL.
- Wire frontend subscription as an isolated hook used only by `event-results-page.tsx`.
- Keep package changes out of the first implementation attempt unless the user explicitly approves `@microsoft/signalr`.
- Run OpenAPI sync if REST results shape changes.
