---
artifact_type: spec
artifact_version: 1
id: spec-20260620200000-role-based-access-control-for-event-operations
title: Role-based access control for event operations
slug: role-based-access-control-for-event-operations
filename_template: 20260620200000-role-based-access-control-for-event-operations.md
created_at: "2026-06-20T20:00:00+07:00"
updated_at: "2026-06-20T20:00:00+07:00"
status: draft
owner: product
tags: [spec, eventhub, accounts-and-identity]
feature_refs: ["F-1.7"]
ddd_refs: ["BC-1", "BC-2", "AGG-User", "AGG-Event"]
prd_refs: ["DEC-3", "QG-1", "QG-5"]
tech_refs: ["Tech §4", "Tech §7"]
db_refs: ["Tech §6"]
github_issue: 17
search_index:
  keywords: [rbac, role-based access control, permissions, authorization, event operations, insufficient permissions, check-in permission, owner, staff]
  bounded_contexts: [Identity and Access, Event Management]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #17 (https://github.com/tranvuongduy2003/eventhub/issues/17)

# Feature: Role-based access control for event operations

> Features: F-1.7  |  Status: DRAFT  |  Date: 2026-06-20
> PRD: DEC-3, QG-1, QG-5  |  DDD: BC-1, BC-2, AGG-Event  |  Tech: §4, §7

## 1. Problem & Solution

**Problem:** F-1.5 defined roles and permissions, and F-1.6 allows assigning roles to users per event, but nothing actually enforces those permissions yet. A Staff user assigned to an event could currently call any event endpoint — edit, publish, cancel, manage tickets — because no operation checks the caller's role. The permissions model exists in the domain but has no teeth.

**Solution:** Introduce an authorization layer that intercepts every protected event operation, looks up the caller's role and its permissions for that event, and either allows the operation to proceed or rejects it with a clear "insufficient permissions" message. Public operations (viewing published event pages, purchasing tickets) remain unaffected.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group organizer) — both need to trust that the permissions they assign are actually enforced.

**Scope:**
- **In:** Permission checks on all protected event operations across Event Management (edit, publish, cancel, add/manage ticket types), Check-in (scan, manual lookup, door counts), and Reporting (attendee list, sales results, export).
- **Out:** Invitation flow (F-1.8), audit log (F-1.9), attendee accounts (F-1.4).

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in and hold the Owner role for an event, WHEN I attempt any protected operation on that event (edit, publish, cancel, manage tickets, check-in, view results), THEN the operation is allowed to proceed.

**AC-02:** GIVEN I am signed in and hold the Staff role with Check-in permission for an event, WHEN I attempt a check-in operation (scan ticket, manual lookup, view door counts), THEN the operation is allowed to proceed.

**AC-03:** GIVEN I am signed in and hold the Staff role with Check-in permission for an event, WHEN I attempt an operation outside Check-in (edit event, publish, cancel, manage tickets, view results/export), THEN I am refused with a clear "insufficient permissions" message and the operation does not execute.

**AC-04:** GIVEN I am signed in but hold no role for an event, WHEN I attempt any protected operation on that event, THEN I am refused with a clear "insufficient permissions" message and the operation does not execute.

**AC-05:** GIVEN I am signed in and hold the Staff role with Reporting permission for an event, WHEN I view the attendee list or sales results for that event, THEN the operation is allowed to proceed.

**AC-06:** GIVEN I am signed in and hold the Staff role with Reporting permission for an event, WHEN I attempt to export the attendee list (an Owner-only operation), THEN I am refused with "insufficient permissions."

**AC-07:** GIVEN anyone (signed in or not), WHEN they access a published event's public page or attempt to purchase tickets, THEN the operation succeeds without any permission check — RBAC does not apply to public operations (EP-4, EP-5).

**AC-08:** GIVEN I am not signed in, WHEN I attempt any protected operation on an event, THEN I receive a 401 Unauthorized response (authentication check happens before authorization).

**AC-09:** GIVEN I am signed in and hold the Owner role for Event A and the Staff role for Event B, WHEN I attempt an Owner-only operation on Event A, THEN it succeeds; WHEN I attempt the same operation on Event B, THEN I am refused with "insufficient permissions."

**AC-10:** GIVEN I am signed in as the Owner for an event, WHEN the event is published, closed, or cancelled, THEN I can still perform operations appropriate to my role and the event's lifecycle state (RBAC does not block based on event state — that is a separate concern handled by the domain invariants).

## 3. Domain & Business Rules

**BR-01 (Permission mapping):** Each protected operation maps to a specific permission from F-1.5. The mapping is stable and documented:

| Operation area | Operations | Required permission |
|---|---|---|
| Event Management | Edit event, add/change ticket types, set cover image, publish, close, cancel, duplicate | `EventManagement` |
| Check-in | Scan ticket, manual lookup, view door counts | `CheckIn` |
| Reporting | View attendee list, view sales results | `Reporting` |
| Export | Export attendee list (CSV) | `Owner` role required (not a Staff permission) |

**BR-02 (Owner bypass):** The Owner role implicitly holds all permissions. An Owner does not need explicit permission entries — their role is sufficient for any operation on that event.

**BR-03 (Public operations exempt):** Viewing a published event page (EP-4) and purchasing tickets (EP-5) are public operations. No role or permission check is performed. This aligns with `feature-specification.md` F-1.7 AC: "Public operations (viewing published event pages, purchasing tickets — EP-4, EP-5) are not affected by RBAC."

**BR-04 (Authentication before authorization):** If the caller is not signed in, the system returns 401 before any permission check. Only authenticated callers reach the authorization layer.

**BR-05 (No role = no access):** A signed-in user with no role assignment for the target event is treated as having zero permissions for that event. They receive "insufficient permissions," not "not found" — the event's existence is not hidden.

**BR-06 (Per-event isolation):** Permissions are scoped to the event. A user's role on Event A has no bearing on Event B. Each operation check looks up the caller's role for the specific event being acted upon.

## 4. API Contract

**Authorization pattern:** Implemented as a **MediatR pipeline behavior** that runs before every handler requiring authorization. Each protected command/query carries metadata indicating the required permission and event identifier (route parameter or inferred from the resource). The behavior resolves the caller's identity from the session, looks up their `EventUserRole` for that event, and checks the required permission. The permission lookup result is **cached per-request scope** to avoid repeated DB calls within a single request.

**Error response (insufficient permissions):**
- Status: `403 Forbidden`
- RFC 7807 body with `code: "INSUFFICIENT_PERMISSIONS"`
- Human-readable detail: "You do not have the required permissions to perform this operation on this event."

**Error response (not authenticated):**
- Status: `401 Unauthorized`
- Standard 401 response (session cookie missing or expired)

**Endpoints affected (existing and upcoming):**

| Endpoint | Method | Required permission | Notes |
|---|---|---|---|
| Edit event | PUT/PATCH | `EventManagement` or `Owner` | Already exists (F-2.3) |
| Publish event | POST | `EventManagement` or `Owner` | Already exists (F-2.4) |
| Close/Cancel event | POST | `EventManagement` or `Owner` | Already exists (F-2.5) |
| Add ticket type | POST | `EventManagement` or `Owner` | Already exists (F-3.1) |
| Change ticket type | PUT | `EventManagement` or `Owner` | Already exists (F-3.5) |
| Scan ticket | POST | `CheckIn` | Already exists (F-8.1) |
| Manual lookup/check-in | POST | `CheckIn` | Already exists (F-8.3) |
| Door counts | GET | `CheckIn` | Already exists (F-8.4) |
| Attendee list | GET | `Reporting` | Already exists (F-9.1) |
| Sales results | GET | `Reporting` | Already exists (F-9.3) |
| Export attendees | GET | `Owner` only | Already exists (F-9.2) |

**Public endpoints (no RBAC):**
| Endpoint | Method | Notes |
|---|---|---|
| View public event page | GET | EP-4 — anyone |
| Purchase / checkout | POST | EP-5 — anyone (guest or signed-in) |

## 5. Data & Storage Impact

**No new tables or schema changes required.** The permission model is already in place from F-1.5:
- `EventUserRole` table (user ↔ event ↔ role) is the source of truth for "who can do what on which event."
- `RolePermission` table (role ↔ permission) defines what each role can do.
- The lookup is: `EventUserRole` for (userId, eventId) → role → `RolePermission` → set of permissions.

**Query pattern:** The authorization check reads the `EventUserRole` for the caller and event, then resolves permissions. This is a read operation; no write is involved. The result is **cached per-request scope** (e.g., via `Scoped` lifetime in DI) so that multiple operations within a single request hit the DB only once.

## 6. Real-Time & Consistency

**N/A.** Authorization is a synchronous, per-request check. It does not involve SignalR or integration events. Changes to a user's role (F-1.6 assign/revoke) take effect on the next request — no push notification is needed for permission changes (that is deferred to F-1.9 audit log scope).

## 7. Security & Privacy

**Session-based identity:** The caller's identity is resolved from the session cookie (F-1.2). The authorization layer trusts the session — it does not re-authenticate.

**Principle of least privilege:** Staff users get only the permissions their role grants. The default Staff role has Check-in permission only; additional permissions must be explicitly assigned via a new role definition or role change.

**No information leakage:** When a user lacks permissions, the error message says "insufficient permissions" — it does not reveal what permissions exist, who else has access, or what the resource's internal state is.

**Public operations are intentionally unprotected:** Viewing published events and purchasing tickets are the product's public-facing surface. These endpoints must remain accessible without authentication or authorization.

## 8. Edge Cases

**EC-01:** A user's role is revoked while they are mid-session (e.g., they have the page open). On their next request, the system looks up their current role and finds none → "insufficient permissions." No session invalidation is needed — the session remains valid (they are still signed in), but their event-level access is gone.

**EC-02:** Ownership is transferred (F-1.6) — the previous Owner is demoted to Staff. If they attempt an Owner-only operation (e.g., export attendees), they are refused. Their session is still valid; only the event-level role changed.

**EC-03:** A user is assigned the Staff role with Reporting permission, then the organizer adds Check-in permission to that role (or assigns a different role). The change takes effect on the next request — no special handling needed.

**EC-04:** An event is cancelled (F-2.5). Protected operations that require a specific lifecycle state (e.g., "cannot edit a cancelled event") are handled by domain invariants, not by RBAC. RBAC only checks "does this user have permission" — the domain checks "is this operation valid given the event's state."

**EC-05:** The same user is signed in on multiple devices. Role changes take effect on the next request from any device — there is no device-specific session state for permissions.

**EC-06:** A Staff user with only Check-in permission tries to view the event's attendee list (Reporting permission). The system checks their role, finds only Check-in permission, and refuses with "insufficient permissions." This is AC-03.

## 9. Dependencies & Risks

**Dependencies:**
- **F-1.5 (Define roles and permissions):** Complete. Provides the permission model (RolePermission, Permission enum).
- **F-1.6 (Assign roles to users per event):** Complete. Provides the user-role-event assignment (EventUserRole).

**Risks:**
- **Performance at scale:** Each protected endpoint now requires a DB lookup for the caller's role. At Next-phase scale (small events, modest concurrency), this is negligible. If it becomes a concern, short-lived caching (per-request or Redis TTL) can be added without changing the contract.
- **Permission granularity:** The current model groups permissions by area (EventManagement, CheckIn, Reporting). If finer-grained control is needed later (e.g., "can edit tickets but not cancel the event"), the model can be extended — but that is out of scope for F-1.7.
- **Consistency with existing endpoints:** All existing protected endpoints will be retrofitted with authorization in this feature. The MediatR pipeline behavior ensures uniform enforcement — no endpoint can be missed.

## 10. Assumptions

- The existing permission model from F-1.5 (Permission enum, RolePermission, EventRole) is sufficient for F-1.7. No new permissions or roles need to be defined.
- The existing EventUserRole assignment from F-1.6 is the source of truth for authorization lookups.
- Authorization is enforced at the Application layer (in or before handlers), not at the Api layer alone — this aligns with the architecture rule that "authorization and ownership checks live in Application handlers."
- The Owner role is not stored as a permission entry; it is a role-level check ("is this user the Owner?") that implicitly grants all permissions.
- Public endpoints (EP-4, EP-5) are explicitly excluded from RBAC and will not be modified by this feature.

## 11. Out of Scope

- **F-1.8 (Invite staff to an event):** Invitation flow is a separate feature.
- **F-1.9 (Permission audit log):** Logging of permission changes is a separate feature.
- **Cross-event permission aggregation:** No "super admin" or global permission — everything is per-event.
- **API key or service-to-service authorization:** Only user session-based auth is considered.
- **Rate limiting or abuse prevention:** Not in scope for this feature.

## 12. Resolved Questions

| # | Question | Resolution |
|---|----------|------------|
| 1 | Should authorization be implemented as a MediatR pipeline behavior or explicit per-handler checks? | **MediatR pipeline behavior** — uniform enforcement, no risk of missing a handler. |
| 2 | Should the permission check result be cached per-request? | **Yes** — cache per-request scope to avoid repeated DB lookups. |
| 3 | Should existing endpoints be retrofitted with authorization now? | **Yes** — add authorization to all existing protected endpoints in this feature. |
