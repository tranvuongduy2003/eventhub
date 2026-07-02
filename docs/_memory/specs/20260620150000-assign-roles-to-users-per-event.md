---
artifact_type: spec
artifact_version: 1
id: spec-20260620150000-assign-roles-to-users-per-event
title: Assign roles to users per event
slug: assign-roles-to-users-per-event
filename_template: 20260620150000-assign-roles-to-users-per-event.md
created_at: "2026-06-20T15:00:00Z"
updated_at: "2026-06-20T15:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, organizer-accounts]
feature_refs: [F-1.6]
ddd_refs: [BC-1, AGG-User, AGG-Event]
prd_refs: [DEC-3, QG-1, QG-5]
tech_refs: [Tech §4, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 15
search_index:
  keywords: [roles, assignment, staff, owner, event, RBAC, permissions, transfer ownership, delegation, check-in]
  bounded_contexts: [Identity and Access]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #15 (https://github.com/tranvuongduy2003/eventhub/issues/15)

# Feature: Assign roles to users per event

> Features: F-1.6  |  Status: DRAFT  |  Date: 2026-06-20
> PRD: DEC-3 (MVP scope), QG-1 (simplicity), QG-5 (correct at small scale)
> DDD: BC-1 (Identity & Access), AGG-User, AGG-Event
> Tech: §4 (CQRS pipeline), §6 (persistence), §7 (API conventions)

## 1. Problem & Solution

**Problem:** The role and permission model (F-1.5) defines Owner and Staff roles, but there is no way for an event owner to actually assign those roles to other users. An organizer who wants a helper to scan tickets at the door has no mechanism to grant that helper the Staff role for a specific event. The role model exists in theory but cannot be used in practice.

**Solution:** Let an event owner assign the Owner or Staff role to any registered user for a specific event. The assignment is per-event — a user can be Staff on one event and Owner on another. Ownership transfer is a special case: assigning a new Owner demotes the previous Owner to Staff, ensuring exactly one Owner per event at all times. Duplicate assignments (same user, same role, same event) are rejected.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group/club organizer)

**Scope:**
- **In:** Assigning roles (Owner, Staff) to registered users for a specific event. Ownership transfer on re-assignment. Revoking a user's role from an event. Listing current role assignments for an event.
- **Out:** Enforcing role-based access on operations (F-1.7), inviting staff by email (F-1.8), audit logging (F-1.9), custom roles.

## 2. Acceptance Criteria

**AC-01:** GIVEN I hold the Owner role for an event and a registered user exists with no role on that event, WHEN I assign that user the Staff role for my event, THEN that user gains Staff permissions (Check-in, Reporting) for that event only, and I remain the Owner.

**AC-02:** GIVEN I hold the Owner role for an event, WHEN I assign a user to a role for my event, THEN that user's role applies only to that event — the user can hold a different role on a different event (e.g., Staff on Event A, Owner on Event B).

**AC-03:** GIVEN I hold the Owner role for an event and another user already holds the Staff role on that event, WHEN I assign that user the Owner role, THEN the previous Owner (me) is demoted to Staff and the target user becomes the new Owner. The event always has exactly one Owner.

**AC-04:** GIVEN a user already holds the Staff role on my event, WHEN I try to assign that same user the Staff role again, THEN the assignment is rejected with a clear message indicating the user already holds that role.

**AC-05:** GIVEN I hold the Owner role for an event, WHEN I revoke a user's role from that event, THEN that user loses all permissions on that event and can no longer perform event operations (until F-1.7 enforces this, the data is correct for future enforcement).

**AC-06:** GIVEN I do NOT hold the Owner role for an event (e.g., I am Staff or have no role), WHEN I try to assign or revoke roles for that event, THEN the operation is rejected — only the Owner can manage role assignments.

**AC-07:** GIVEN I hold the Owner role for an event, WHEN I try to assign a role to a user who does not exist, THEN the operation is rejected with a clear "user not found" message.

**AC-08:** GIVEN I hold the Owner role for an event, WHEN I view the list of role assignments for that event, THEN I see each user's name, email, and assigned role.

## 3. Domain & Business Rules

Reference: `domain-model-specification.md` BC-1 (Identity & Access), BC-2 (Event Management). The role assignment model bridges the User aggregate (identity) and the Event aggregate (context).

**BR-01 — Assignment is per-event:** A role assignment is a triple of (UserId, EventId, Role). A user's role on one event has no bearing on their role on another event.

**BR-02 — Exactly one Owner per event:** An event must have exactly one Owner at all times. When a new Owner is assigned, the previous Owner is automatically demoted to Staff. This is an invariant of the assignment operation.

**BR-03 — No duplicate assignments:** A user cannot hold the same role twice on the same event. Attempting to assign a role the user already holds is rejected.

**BR-04 — Only Owner can assign/revoke:** The assignment and revocation operations require the caller to hold the Owner role on the target event. This is a precondition check, not an aggregate invariant — it is enforced by the application handler (Constitution II.7).

**BR-05 — Assignment creates access, revocation removes it:** Assigning a role grants the permissions of that role immediately. Revoking a role removes all permissions immediately. The effect is synchronous and transactional.

**BR-06 — Ownership transfer is atomic:** When the Owner role is assigned to a new user, the demotion of the previous Owner and the promotion of the new Owner happen in the same transaction. There is no moment where the event has zero Owners or two Owners.

**BR-07 — Revoking the current Owner is not allowed:** The Owner cannot revoke their own role without first transferring ownership to another user. An event must always have an Owner.

**BR-08 — Self-assignment is not allowed:** A user cannot assign themselves a role (they already hold Owner on events they created; assigning themselves Staff would violate the one-role-per-user-per-event constraint).

## 4. UI Behavior **or** API Contract

**API endpoints (product-level contract):**

| Operation | Method / Path | Request | Response |
|-----------|---------------|---------|----------|
| Assign role | `POST /api/events/{eventId}/roles` | `{ "userId": "<id>", "role": "Staff" }` | `201` — the created assignment |
| Revoke role | `DELETE /api/events/{eventId}/roles/{userId}` | — | `204` — no body |
| List assignments | `GET /api/events/{eventId}/roles` | — | `200` — array of assignments with user details |

**Ownership transfer:** When the request assigns the Owner role to another user, the API behaves the same as a normal assignment — the caller's role changes to Staff automatically. The response reflects the new state (the caller is now Staff, the target is Owner).

**Error responses (RFC 7807):**
- Caller is not the Owner → `403` with code `INSUFFICIENT_PERMISSIONS`
- Target user not found → `404` with code `USER_NOT_FOUND`
- Duplicate assignment (same user, same role) → `409` with code `ROLE_ALREADY_ASSIGNED`
- Caller tries to revoke themselves as Owner without transferring → `422` with code `CANNOT_REVOKE_OWNER`

**UI (organizer-facing):**
- A "Team" or "Staff" section on the event management page where the Owner can see current assignments and add/remove users.
- A simple form: enter a user's email (looked up to resolve UserId), select a role (Staff by default, Owner available), confirm.
- When transferring ownership, a confirmation step explaining that the current owner will become Staff.

## 5. Data & Storage Impact

**PostgreSQL (`app` schema):**

The `EventUserRole` table introduced in F-1.5 stores role assignments. For F-1.6, this table is actively read and written:

| Table | Columns | Notes |
|-------|---------|-------|
| `event_user_role` | `event_id` (FK), `user_id` (FK), `role` (enum: Owner/Staff), `created_at` | Composite PK on `(event_id, user_id)` — one role per user per event. `created_at` for future audit (F-1.9). |

- **Index:** `(event_id)` for listing all assignments for an event. `(user_id)` for finding all events a user has a role on.
- **Unique constraint:** `(event_id, user_id)` — enforces one role per user per event at the database level.
- **Enum:** `role` column uses a PostgreSQL enum or string check constraint with values `Owner` and `Staff`.

**Redis:** No impact for this slice. Role lookups during operation enforcement (F-1.7) may cache assignments later.

**MinIO:** No impact.

**RabbitMQ:** No impact. Role changes could emit integration events for audit logging (F-1.9) in the future, but that is out of scope here.

## 6. Real-Time & Consistency

**N/A** for this slice. Role assignments are synchronous, request-scoped operations.

**Consistency:** Ownership transfer (assigning a new Owner demotes the previous one) must be transactional — both updates succeed or both fail. This is within the same unit of work since both rows are in the same table.

## 7. Security & Privacy

**SEC-01 — Caller authorization:** Only the event Owner can assign or revoke roles. The handler must verify the caller holds the Owner role before executing. This is an application-layer check (Constitution II.7), not middleware.

**SEC-02 — No self-promotion:** A Staff user cannot assign themselves the Owner role. The Owner-only precondition prevents this.

**SEC-03 — Ownership transfer is intentional:** Transferring ownership is a destructive action for the current owner (they lose full control). The API should require the caller to explicitly request the Owner role for the target — it should not happen accidentally.

**SEC-04 — Guest access unaffected:** Role assignment is an organizer-side operation. Public operations (viewing events, purchasing tickets) are not affected.

**SEC-05 — User enumeration:** The assign-role endpoint accepts a user identifier. If the system returns "user not found" for invalid identifiers, this could be used to enumerate registered emails. For the MVP, this is an accepted trade-off; mitigations (rate limiting, generic error messages) can be added later.

## 8. Edge Cases

**EC-01:** GIVEN the event has one Owner, WHEN that Owner tries to revoke their own role without first transferring ownership to another user, THEN the operation is rejected — an event must always have an Owner.

**EC-02:** GIVEN two Owners exist briefly due to a race condition (concurrent assignment requests), WHEN the system processes both, THEN the optimistic concurrency mechanism ensures only one succeeds — the second request sees the conflict and retries or fails cleanly.

**EC-03:** GIVEN a user is Staff on an event, WHEN the Owner assigns them the Owner role (ownership transfer), THEN the user's role is updated from Staff to Owner (not a new row inserted), and the previous Owner becomes Staff.

**EC-04:** GIVEN a user has no role on an event, WHEN the Owner revokes that user's role, THEN the operation is a no-op or returns a clear message — there is nothing to revoke.

**EC-05:** GIVEN the Owner assigns another user as Owner (transfer), WHEN I check the previous Owner's role, THEN they are now Staff — not removed from the event entirely. They retain Check-in and Reporting permissions.

**EC-06:** GIVEN the event is Draft (not yet published), WHEN the Owner assigns roles, THEN the assignment succeeds — roles can be assigned at any event lifecycle stage.

## 9. Dependencies & Risks

**Dependencies:**
- F-1.1 (Register an organizer account) — the target user must exist.
- F-1.5 (Define roles and permissions) — the role model (Owner, Staff) and permission sets must be defined.
- F-2.1 (Create a draft event) — the event must exist and have an Owner (the creator, per F-1.5).

**Risks:**
- **RSK-R1 — Ownership transfer edge cases:** Transferring ownership is a sensitive operation with several edge cases (self-transfer, transfer to non-existent user, concurrent transfers). The spec defines the expected behavior, but implementation must handle these carefully.
- **RSK-R2 — UI simplicity:** The assignment UI must stay simple (email lookup + role selection). Avoid building a full user management dashboard — that is enterprise scope, not MVP.
- **RSK-R3 — Coupling with F-1.7:** This feature creates the data (role assignments) that F-1.7 will enforce. If the data model is wrong, F-1.7 enforcement will be awkward. The composite-key design (event_id, user_id, role) is straightforward and should not cause issues.

## 10. Assumptions

- **ASM-R1:** The target user must be a registered organizer (F-1.1). Assigning roles to non-existent or attendee-only users is not supported.
- **ASM-R2:** A user holds exactly one role per event. There is no concept of "both Owner and Staff" — the system assigns one role at a time.
- **ASM-R3:** Role assignment is synchronous and does not require email confirmation. The assigned user gains permissions immediately. Email-based invitations are F-1.8.
- **ASM-R4:** The API accepts a user identifier (UserId or email) to identify the target user. The spec leaves the exact identifier to implementation.
- **ASM-R5:** Revoking a role removes the user from the event entirely (they have no role). There is no "inactive" or "suspended" state.

## 11. Out of Scope

- Enforcing role-based access on operations (F-1.7)
- Inviting staff by email with invitation workflow (F-1.8)
- Permission audit logging (F-1.9)
- Custom or user-defined roles
- Bulk role assignment (assigning multiple users at once)
- Role assignment for attendees (attendees do not hold roles)
- Notifications when a role is assigned or revoked

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the assign-role API accept a UserId (GUID) or an email address to identify the target user? | ❓ Email is more natural for the caller (organizer types an email), but UserId is simpler at the API level. Recommend: accept email, resolve to UserId server-side. |
| 2 | When ownership is transferred, should the previous Owner receive a notification? | ❓ Out of scope for MVP (Notifications is BC-6). Could be added as a domain event consumer later. |
| 3 | Should the `EventUserRole` table have a separate unique constraint on `(event_id, role)` where `role = 'Owner'` to enforce one-Owner-per-event at the database level? | ❓ This adds DB-level safety but complicates ownership transfer (must delete + insert in one transaction). The application invariant (BR-02) may be sufficient. |
| 4 | Should revoking a role be a soft delete (add `revoked_at` column) or a hard delete from the table? | ❓ Hard delete is simpler for MVP. Soft delete is useful for audit (F-1.9) but can be added later without breaking the model. |
