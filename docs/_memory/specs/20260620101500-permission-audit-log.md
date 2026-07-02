---
artifact_type: spec
artifact_version: 1
id: spec-20260620101500-permission-audit-log
title: Permission Audit Log
slug: permission-audit-log
filename_template: 20260620101500-permission-audit-log.md
created_at: "2026-06-20T10:15:00Z"
updated_at: "2026-06-20T10:15:00Z"
status: implemented
owner: product
tags: [spec, eventhub, organizer-accounts-identity]
feature_refs: ["F-1.9"]
ddd_refs: ["BC-1", "BC-7"]
prd_refs: ["QG-1", "QG-5", "G-5"]
tech_refs: ["Tech §4", "Tech §6", "Tech §7"]
db_refs: ["Tech §6"]
github_issue: 21
search_index:
  keywords: [audit, log, role, permission, assignment, accountability, organizer, event, immutable]
  bounded_contexts: [Identity and Access, Reporting and Audience]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #21 (https://github.com/tranvuongduy2003/eventhub/issues/21)

# Feature: Permission Audit Log

> Features: F-1.9  |  Status: DRAFT  |  Date: 2026-06-20
> PRD: G-5 (organizer clarity and ownership), QG-1 (simplicity), QG-5 (correctness)
> DDD: BC-1 (Identity & Access), BC-7 (Reporting & Audience)
> Tech: §4 (CQRS pipeline), §6 (persistence), §7 (API conventions)

## 1. Problem & Solution

**Problem:** When multiple organizers or staff share access to an event, there is no record of who changed what role and when. If someone's permissions are unexpectedly altered, the organizer has no way to investigate. This undermines trust and accountability, especially for PER-O2 (small groups/clubs) where several people manage the same event.

**Solution:** An immutable audit log that records every role assignment, revocation, and ownership transfer for an event. Each entry captures the actor, the target user, the event, the action, the roles involved, and the timestamp. Organizers can view and filter the log for events they own.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group/club organizer)

**Scope:**
- **In:** F-1.9 — audit entries for role assignments, revocations, and transfers; viewing and filtering the audit log.
- **Out:** Audit for non-role operations (event edits, ticket changes); attendee-side visibility; export of audit data.

## 2. Acceptance Criteria

**AC-01:** GIVEN an event I own, WHEN a role is assigned to a user for that event (F-1.6), THEN an audit entry is created recording: the acting user (who made the change), the target user (who received the role), the event, the action type ("assigned"), the new role, and a UTC timestamp.

**AC-02:** GIVEN an event I own, WHEN a user's role is revoked for that event, THEN an audit entry is created recording: the acting user, the target user, the event, the action type ("revoked"), the old role that was removed, and a UTC timestamp.

**AC-03:** GIVEN an event I own, WHEN ownership is transferred to another user (F-1.6 — assigning a new Owner demotes the previous), THEN one or two audit entries are created recording: the acting user, the previous owner (demoted to Staff), the new owner (promoted from Staff or unassigned), the event, the action types ("transferred" or "assigned"/"revoked" as appropriate), the old and new roles, and UTC timestamps.

**AC-04:** GIVEN I hold the Owner role for an event, WHEN I view the audit log for that event, THEN I see all audit entries for that event, each showing: the acting user's display name, the target user's display name, the action type, the old role (if any), the new role (if any), and the timestamp.

**AC-05:** GIVEN I am viewing an audit log, WHEN I filter by a date range, THEN only entries whose timestamp falls within that range are shown.

**AC-06:** GIVEN I am viewing an audit log, WHEN I filter by action type (assigned, revoked, transferred), THEN only entries matching that action type are shown.

**AC-07:** GIVEN I hold the Owner role for an event, WHEN I attempt to edit or delete an audit entry, THEN the operation is refused — audit entries are immutable once created.

**AC-08:** GIVEN I do not hold the Owner role for an event, WHEN I attempt to view that event's audit log, THEN I am refused with an "insufficient permissions" message.

**AC-09:** GIVEN a role change occurs, WHEN the audit entry is created, THEN the entry is persisted in the same transaction as the role change itself, ensuring no role change exists without a corresponding audit record.

**AC-10:** GIVEN I hold a Staff role for an event, WHEN I attempt to view the audit log for that event, THEN I am refused with an "insufficient permissions" message — only Owners can view audit logs.

## 3. Domain & Business Rules

**Bounded context:** The audit log is a supporting capability in **BC-1 (Identity & Access)**. It records changes to event-level role assignments managed by F-1.6. The read-side projection for viewing the log may live in **BC-7 (Reporting & Audience)** as a read model, consistent with how other reporting features are structured.

**Invariants:**
- An audit entry, once written, cannot be modified or deleted. This is a hard immutability constraint.
- Every role-changing operation (assign, revoke, transfer) must produce at least one audit entry in the same transaction. No role change without a record.
- Only the event's Owner can view the audit log. Staff and other roles are excluded.

**Action types (enum):**
- `Assigned` — a user received a role for an event.
- `Revoked` — a user's role was removed for an event.
- `Transferred` — ownership moved from one user to another (the previous owner is demoted, the new owner is promoted).

**Relationship to existing aggregates:**
- The audit entry references a `UserId` (actor), a `UserId` (target), and an `EventId` — all by identity, never by holding aggregate instances (per `domain-model-specification.md` §3).
- The audit entry is not part of the `User` or `Event` aggregate; it is its own record type, written as a side effect of role-changing operations.

## 4. API Contract

**Endpoints (REST):**

| Method | Path | Purpose | Auth |
|--------|------|---------|------|
| `GET` | `/api/events/{eventId}/audit-log` | View the audit log for an event | Owner role required |

**Query parameters for `GET /api/events/{eventId}/audit-log`:**
- `from` (optional, ISO-8601 datetime) — start of date range filter (inclusive).
- `to` (optional, ISO-8601 datetime) — end of date range filter (inclusive).
- `action` (optional, enum: `assigned`, `revoked`, `transferred`) — filter by action type.
- `page` (optional, integer, default 1) — pagination page number.
- `pageSize` (optional, integer, default 20, max 100) — entries per page.

**Response (200):** A paginated list of audit entries, each containing:
- `id` — unique identifier for the audit entry.
- `actorName` — display name of the user who performed the action.
- `targetName` — display name of the user whose role was changed.
- `action` — one of `assigned`, `revoked`, `transferred`.
- `oldRole` (nullable) — the role before the change, if applicable.
- `newRole` (nullable) — the role after the change, if applicable.
- `occurredAt` — UTC timestamp of the change.

**Error responses:**
- `401` — not signed in.
- `403` — signed in but not the Owner of the event.
- `404` — event not found.

## 5. Data & Storage Impact

**PostgreSQL (`app` schema):**
- A new table for permission audit entries. Each row is an immutable record with foreign-key references to the acting user, the target user, and the event.
- Index on `(event_id, occurred_at)` to support efficient filtering and pagination.
- No `row_version` needed — entries are insert-only, never updated.
- No Redis caching required — audit log is a low-traffic, infrequently queried feature.

**Write path:** The audit entry is written in the same unit of work as the role assignment/revocation/transfer operation (AC-09), ensuring atomicity.

**No MinIO or RabbitMQ impact.** Audit entries are written synchronously alongside role changes and read directly from PostgreSQL.

## 6. Real-Time & Consistency

**N/A.** The audit log does not require real-time push. Entries are written transactionally with role changes (strong consistency) and read on demand. No SignalR or integration events are needed for this feature.

## 7. Security & Privacy

- **Access control:** Only the event's Owner can view the audit log (AC-08, AC-10). This is enforced in the Application handler via `ICurrentUserAccessor` and role verification, consistent with F-1.7.
- **Immutability:** Audit entries cannot be edited or deleted by any user, including the Owner. This is enforced at the data layer (no update/delete operations exposed).
- **Data minimization:** The audit log stores display names (not emails or other PII beyond what is already visible to the Owner through the event's staff list). Actor and target are identified by `UserId`; display names are denormalized for convenience but remain within the data the Owner already has access to.
- **Session auth:** The endpoint uses the same cookie-session authentication as other event management endpoints.

## 8. Edge Cases

**EC-01:** A role assignment fails (e.g., duplicate assignment per F-1.6). No audit entry is created because no role change occurred.

**EC-02:** Ownership transfer involves two changes (old owner demoted, new owner promoted). The audit log records this as a `transferred` action, potentially with two entries if the demotion and promotion are modeled as separate operations. The entries share the same transaction timestamp.

**EC-03:** An event is cancelled (F-2.5). The audit log remains viewable for the Owner; cancellation does not delete history.

**EC-04:** A user who was assigned a role is later deleted or deactivated. The audit entry persists with the target user's display name at the time of the action. The entry is still viewable.

**EC-05:** No audit entries exist for an event (fresh event, no role changes). The log view returns an empty list, not an error.

**EC-06:** Date range filter with `from` after `to`. The endpoint returns an empty list (no entries match an impossible range), not an error.

## 9. Dependencies & Risks

**Dependencies:**
- F-1.6 (assign roles to users per event) — the operations being audited.
- F-1.7 (role-based access control) — the permission model that the audit log's access control builds on.

**Risks:**
- **Storage growth:** For a pet project with small events, this is negligible. Audit entries are small rows; even hundreds of role changes per event would not be a concern.
- **Transaction coupling:** Writing the audit entry in the same transaction as the role change adds a small amount of complexity to those handlers, but this is the correct trade-off for AC-09 (no orphaned role changes).

## 10. Assumptions

- Display names are sufficient for the audit log; no need to snapshot email or other user details.
- The audit log does not need to record who viewed it (read-access auditing is out of scope).
- Pagination is acceptable for viewing; no need for real-time streaming of audit entries.
- The feature is scoped to event-level role changes only. Account-level changes (password change, profile update) are out of scope.

## 11. Out of Scope

- Audit for non-role operations (event edits, ticket type changes, order operations).
- Export of audit data (CSV, PDF).
- Attendee-facing audit visibility.
- Read-access auditing (who viewed the log).
- Cross-event audit aggregation or global audit dashboards.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the transfer action produce one audit entry ("transferred") or two separate entries ("revoked" for old owner + "assigned" for new owner)? A single entry is simpler; two entries are more granular. | ✅ **Two entries** — `Transferred` for demoted old owner + `Assigned` for new owner. More granular audit trail. Implemented in `AssignRoleCommandHandler`. |
| 2 | Should the audit log be accessible to Staff with a specific permission (e.g., a "View Audit" permission), or strictly Owner-only? Current spec assumes Owner-only for simplicity (QG-1). | ✅ **Owner-only** — uses `Permission.EventManagement` (Owner has all permissions, Staff has CheckIn + Reporting only). Implemented in `ListAuditLogQuery`. |
