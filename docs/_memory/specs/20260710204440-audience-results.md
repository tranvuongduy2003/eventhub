---
title: Audience & Results
type: implementation-spec
status: implemented
feature_ids:
  - F-9.1
  - F-9.2
  - F-9.3
  - F-9.4
  - F-9.5
  - F-9.6
epic: EP-9
created: 2026-07-10
updated_at: 2026-07-10
---

# Audience & Results

## Problem And Solution

Organizers need a simple way to own their audience relationship after tickets are sold and scanned. EventHub already creates orders, issues tickets, and records check-ins; this slice turns those records into organizer-facing attendee lists, exports, event results, overview stats, and light attendee messaging.

The solution is an authorized reporting and audience surface per event, plus an organizer overview. Owners and staff with Reporting permission can view attendee and results data. Only owners can export attendee data or send attendee messages. Optional reminders are owner-controlled and delivered through the same asynchronous email path as ticket delivery.

## Acceptance Criteria

- `F-9.1` Given I hold the Owner or Staff role with Reporting permission for the event, I can see each attendee's name, email, ticket type, order, and check-in status.
- `F-9.1` A user without Reporting permission is refused with an "insufficient permissions" message.
- `F-9.2` Given I hold the Owner role for the event, when I export, then I get a CSV of the attendee list for my event.
- `F-9.2` A user without the Owner role, including Staff, is refused with an "insufficient permissions" message.
- `F-9.3` Given I hold the Owner or Staff role with Reporting permission for the event, I can see tickets sold by type, total revenue, check-in rate, and no-shows.
- `F-9.3` A user without Reporting permission is refused with an "insufficient permissions" message.
- `F-9.4` Given I am signed in, I see events where I hold the Owner role with sold count, revenue, date, status, and a way to open results.
- `F-9.4` Events where I hold only the Staff role appear separately with check-in stats only.
- `F-9.5` Given I hold the Owner role for the event, when I send a message to attendees, then it is delivered to attendee emails asynchronously.
- `F-9.5` A user without the Owner role, including Staff, is refused with an "insufficient permissions" message.
- `F-9.6` Given I hold the Owner role for the event, when I enable a reminder, then attendees receive an email a set time before the event.
- `F-9.6` A user without the Owner role is refused with an "insufficient permissions" message.

## Domain And Business Rules

- Reporting & Audience is a read-model context. It derives from confirmed orders, issued tickets, ticket types, and check-in state; it must not mutate those aggregates.
- Attendee rows represent issued tickets, not pending order reservations. A multi-ticket order may produce multiple attendee rows.
- Revenue is gross order revenue because EventHub has no platform fee.
- No-shows are issued tickets that have not been checked in.
- Check-in rate is checked-in tickets divided by issued tickets. If no tickets are issued, the rate is zero.
- Attendee export is owner-only because it moves audience data outside EventHub.
- Attendee messages and reminders are owner-only because they contact the organizer's audience.

## UI Behavior Or API Contract

- The organizer can open an event results view from their organizer area.
- The attendee list shows name, email, ticket type, order reference, ticket identifier, and check-in state.
- The results view shows tickets sold by type, total revenue, check-in rate, and no-shows.
- The organizer overview separates owned events from staff-assigned events. Owned events show sales and revenue stats; staff events show check-in progress only.
- Export returns a CSV file with the same attendee fields visible in the attendee list.
- Message sending accepts a subject and body and returns a confirmation that delivery has been queued.
- Reminder settings let the owner enable or disable a reminder and choose a supported lead time.

## Data

- Source data is PostgreSQL-backed order, order-line, ticket, ticket-type, event, and event-role data.
- Messaging uses the existing email abstraction and asynchronous delivery pattern. Email delivery must be idempotent enough to tolerate retries.
- Reminder settings are authoritative product state and must be persisted.

## Real-Time

No live updates are required in this slice. Realtime monitoring remains in EP-11.

## Security

- Authorization checks live in Application command/query handlers.
- Owner role satisfies all reporting, export, messaging, and reminder checks.
- Staff must have Reporting permission to read attendee and results data.
- Staff cannot export, send messages, or configure reminders.
- Unauthenticated users are refused by protected API routes and application checks.

## Edge Cases

- Events with no issued tickets return empty attendee lists and zero-valued results.
- Mixed free and paid tickets count toward sold and attendance; revenue only sums captured/confirmed gross amounts.
- Cancelled or refunded orders must not inflate active attendance or revenue once those states exist.
- CSV fields that contain commas, quotes, or line breaks are escaped.
- A message to an event with no attendees is accepted only if the product deliberately treats it as a no-op; otherwise it returns a clear validation message.
- Reminder lead times that would schedule after the event starts are rejected.

## Dependencies

- Depends on EP-5 through EP-8 records: confirmed orders, attendees, tickets, and check-ins.
- Depends on F-1.7 authorization for reporting, owner-only export, messaging, and reminder settings.
- Depends on the existing email delivery abstraction for attendee messaging and reminders.

## Assumptions

- This slice may use current authoritative tables directly if persistent reporting projections are not yet present, while preserving the Reporting & Audience bounded-context language at the Application/API boundary.
- The MVP email adapter may be no-op in local development, but the Application behavior still queues/sends through the email port.
- Reminder background scheduling can be implemented with the repository's existing background-job style rather than adding a new infrastructure dependency.

## Out Of Scope

- Realtime sales or check-in updates, which belong to EP-11.
- Advanced segmentation, templates, unsubscribe preferences, campaign analytics, or marketing automation.
- Platform-level audience exports across multiple events.

## Adjacent Feature Boundary

- In scope for `F-9.1` through `F-9.4`: attendee list, CSV export, event results, and organizer overview derived from existing order, ticket, and check-in records.
- In scope for `F-9.5` and `F-9.6`: owner-only light attendee email and automatic event reminders using the existing email/background delivery surface.
- Adjacent to EP-8 check-in: this slice reads check-in state but does not change scan, lookup, or door-count behavior.
- Adjacent to EP-11 realtime: this slice exposes static results and overview data only; live pushes remain out of scope.
- Adjacent to EP-10 transfer/returns: transfer and return behavior is out of scope; reporting should not block future updates to attendee holder data.

## 7. Harness Impact

- `harness/evals/`: N/A - product slice only; no harness behavior changes.
- `harness/orchestrator/`: N/A - product slice only; no harness behavior changes.
- `.codex/policies/` and `harness/policies/`: N/A - product slice only; no harness behavior changes.
- `harness/telemetry/`: N/A - product slice only; no harness behavior changes.
- `harness/tools/`: N/A - product slice only; no harness behavior changes.
- Workflow surfaces (`harness/graph/`, `.agents/skills/`, `.codex/hooks/`, `scripts/agent/`, `AGENTS.md`): N/A - product slice only; no harness behavior changes.
