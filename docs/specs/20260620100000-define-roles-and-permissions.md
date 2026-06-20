---
artifact_type: spec
artifact_version: 1
id: spec-20260620100000-define-roles-and-permissions
title: Define roles and permissions
slug: define-roles-and-permissions
filename_template: 20260620100000-define-roles-and-permissions.md
created_at: "2026-06-20T10:00:00Z"
updated_at: "2026-06-20T10:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, organizer-accounts]
feature_refs: [F-1.5]
ddd_refs: [BC-1, AGG-User]
prd_refs: [DEC-3, QG-1, QG-5]
tech_refs: [Tech §4, Tech §7]
db_refs: [Tech §6]
github_issue: 13
search_index:
  keywords: [roles, permissions, owner, staff, RBAC, event access, check-in, authorization, capability]
  bounded_contexts: [Identity and Access]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #13 (https://github.com/tranvuongduy2003/rebuilt-eventhub/issues/13)

# Feature: Define roles and permissions

> Features: F-1.5  |  Status: DRAFT  |  Date: 2026-06-20
> PRD: DEC-3 (MVP scope), QG-1 (simplicity), QG-5 (correct at small scale)
> DDD: BC-1 (Identity & Access), AGG-User
> Tech: §4 (CQRS pipeline), §6 (persistence), §7 (API conventions)

## 1. Problem & Solution

**Problem:** Without a role system, every organizer who creates an event has unrestricted access, and there is no way to delegate limited responsibilities (like door check-in) to helpers without giving full control. A small event team needs a clear separation between "the person who owns and manages the event" and "the person who only scans tickets at the door."

**Solution:** Establish a built-in role model with two roles — **Owner** and **Staff** — each carrying a distinct, non-overlapping set of permissions. Permissions are grouped by capability area (Event Management, Ticketing, Check-in, Reporting, Staff Management). The Owner has full control; Staff is limited to check-in operations and viewing attendee lists. This role model is the foundation that F-1.6 (assign roles per event) and F-1.7 (enforce RBAC on operations) build upon.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group/club organizer)

**Scope:**
- **In:** Defining the two roles (Owner, Staff), their permission sets, and the grouping by capability area. Storing the creator of an event as its initial Owner.
- **Out:** Assigning roles to other users (F-1.6), enforcing permissions on operations (F-1.7), inviting staff by email (F-1.8), audit logging (F-1.9).

## 2. Acceptance Criteria

**AC-01:** GIVEN the system is running, WHEN I inspect the available roles, THEN at least two roles exist: **Owner** (full control over the event — create, edit, publish, cancel, manage tickets, manage staff, check-in, view results) and **Staff** (limited to check-in operations and viewing attendee lists for assigned events).

**AC-02:** GIVEN the permission model, WHEN I examine the permission groupings, THEN permissions are organized into five capability areas: **Event Management** (create, edit, publish, cancel events), **Ticketing** (define and manage ticket types), **Check-in** (scan and validate tickets at the door, view door counts), **Reporting** (view attendee lists, sales results, check-in stats), and **Staff Management** (assign and revoke roles for the event).

**AC-03:** GIVEN the Owner role, WHEN I check its permissions, THEN the Owner holds all permissions across all five capability areas — Event Management, Ticketing, Check-in, Reporting, and Staff Management.

**AC-04:** GIVEN the Staff role, WHEN I check its permissions, THEN Staff holds only **Check-in** (scan tickets, view door counts) and **Reporting** (view attendee lists) permissions. Staff does NOT hold Event Management, Ticketing, or Staff Management permissions.

**AC-05:** GIVEN an organizer creates a new event (F-2.1), WHEN the event is created, THEN the creating organizer is automatically assigned the Owner role for that event.

**AC-06:** GIVEN the permission model, WHEN I compare role permissions to account-level capabilities, THEN creating events is an account-level capability (any signed-in organizer can create events), not a per-event permission. Role permissions govern what a user can do on a specific event they are associated with.

**AC-07:** GIVEN the role definitions, WHEN I inspect the data model, THEN roles and permissions are stored as part of the domain model and are not hardcoded as magic strings scattered across the codebase — they are defined in one authoritative location.

## 3. Domain & Business Rules

Reference: `ddd.md` BC-1 (Identity & Access), the permission model extends the User aggregate's relationship to events.

**BR-01 — Role as a domain concept:** A Role is a named set of permissions that governs what a user can do on a specific event. Roles are assigned per event, not globally (a user can be Owner on one event and Staff on another).

**BR-02 — Permission as a value object:** A Permission is an immutable value representing a specific capability (e.g., `EventManagement`, `Ticketing`, `CheckIn`, `Reporting`, `StaffManagement`). A Role is a collection of Permissions.

**BR-03 — Owner is all-powerful within its event:** The Owner role implicitly includes every permission. Adding a new permission area in the future automatically grants it to Owners.

**BR-04 — Staff is deliberately limited:** Staff is a restricted role for helpers at the door. It cannot modify the event, change ticket configurations, or manage other staff members.

**BR-05 — Permission areas are non-overlapping:** Each permission belongs to exactly one capability area. A user either has a capability area's permission or does not — there are no partial or conditional permissions within an area.

**BR-06 — System-level vs event-level:** Creating events is a system-level capability available to any signed-in organizer. Once an event exists, role-based permissions govern actions on that specific event. These are separate concerns.

**BR-07 — Creator becomes Owner automatically:** When an event is created, the creating organizer is assigned the Owner role for that new event. This is a side effect of event creation, not a separate step.

## 4. UI Behavior **or** API Contract

This feature is primarily a domain model concern. The UI and API surface is minimal for this slice — the roles and permissions exist in the system and are used by subsequent features (F-1.6 assignment, F-1.7 enforcement).

**API (informational — no new endpoints for this slice):**
- The roles and permissions model is internal to the system. No public endpoint is needed to "list roles" in the MVP — the two roles are built-in and well-known.
- When F-1.6 adds role assignment, the API will accept role identifiers (e.g., `"Owner"`, `"Staff"`) as part of the assignment payload.

**UI (informational — no new screens for this slice):**
- Role names and permission descriptions will appear in the UI when F-1.6 (assign roles) and F-1.7 (access denied messages) are built.
- The event creation flow (F-2.1) silently assigns the creator as Owner — no extra UI step.

## 5. Data & Storage Impact

**PostgreSQL (`app` schema):**
- A new table or columns are needed to represent event-level role assignments. At minimum: an association table linking `UserId`, `EventId`, and `Role` (as an enum or reference).
- For this slice (F-1.5 only), the model defines the roles and permissions; the assignment table is created when F-1.6 is implemented. However, the automatic Owner assignment on event creation (AC-05) requires that the association can be written, so the storage for role assignments should be introduced here.
- Recommended: an `EventUserRole` table with `(EventId, UserId, Role)` as the composite key, ensuring a user holds exactly one role per event.

**Redis:** No impact — roles are not cached in this slice.

**MinIO:** No impact.

**RabbitMQ:** No impact.

## 6. Real-Time & Consistency

**N/A** for this slice. Role assignments are written synchronously during event creation. Real-time updates for role changes are a concern for later features (EP-11).

**Consistency:** The automatic Owner assignment on event creation (AC-05) must be part of the same transaction as the event creation itself — if the event is created but the Owner assignment fails, the system would have an ownerless event. This is a strong-consistency requirement within the same aggregate boundary.

## 7. Security & Privacy

**SEC-01 — Authorization foundation:** This feature establishes the permission model that F-1.7 will enforce. Until F-1.7 is implemented, no operation is actually gated by roles — but the model must be correct so enforcement can be added without a redesign.

**SEC-02 — No privilege escalation:** A Staff user cannot grant themselves Owner-level permissions. This is enforced by the assignment mechanism (F-1.6), but the role model must make it structurally impossible for Staff to hold Owner permissions.

**SEC-03 — Guest access unaffected:** Public operations (viewing published event pages, purchasing tickets) are not governed by roles. Roles apply only to organizer-side operations on events.

## 8. Edge Cases

**EC-01:** GIVEN an event has no Owner (data corruption or failed assignment), WHEN any owner-gated operation is attempted, THEN the system should reject it with a clear message. The event creation flow must guarantee an Owner exists, but defensive checks are warranted.

**EC-02:** GIVEN the system defines only Owner and Staff roles, WHEN a future feature requires a new permission area (e.g., "Messaging" for EP-11), THEN adding the permission to the model should automatically include it in the Owner role and exclude it from Staff, without requiring data migration for existing assignments.

**EC-03:** GIVEN a user creates multiple events, WHEN I check their roles, THEN they are Owner on each event they created, independently. Roles are per-event, not global.

**EC-04:** GIVEN the permission model, WHEN I check whether permissions are ordered or hierarchical, THEN they are not — having Check-in permission does not imply Reporting permission. Each permission is independent and explicitly granted through the role.

## 9. Dependencies & Risks

**Dependencies:**
- F-1.1 (Register an organizer account) — must exist; roles are assigned to registered users.
- F-2.1 (Create a draft event) — triggers the automatic Owner assignment (AC-05). The role model can be defined independently, but the Owner-on-creation behavior requires F-2.1.

**Risks:**
- **RSK-R1 — Over-engineering the permission model:** With only two roles and five permission areas, the model must stay simple. Avoid building a full permission matrix, custom role creation UI, or fine-grained per-action permissions — those are enterprise features out of scope for a small-event platform (prd.md QG-1).
- **RSK-R2 — Coupling with F-1.6 and F-1.7:** This spec defines the model; F-1.6 adds assignment; F-1.7 adds enforcement. If the model is too rigid, F-1.7 enforcement will be awkward. The five-area grouping is designed to be extensible without being over-general.

## 10. Assumptions

- **ASM-R1:** The two roles (Owner, Staff) are sufficient for the MVP. Additional roles (e.g., "Co-organizer" with intermediate permissions) are out of scope and can be added later without breaking the model.
- **ASM-R2:** Permission areas are coarse-grained (five areas), not fine-grained per-action. "Event Management" is one permission area, not separate "edit title", "edit description", etc.
- **ASM-R3:** The automatic Owner assignment on event creation is unconditional — the creator always becomes Owner. Ownership transfer (F-1.6) is a separate operation.
- **ASM-R4:** This spec does not introduce new API endpoints. The role model is an internal domain concern consumed by F-1.6 and F-1.7.

## 11. Out of Scope

- Assigning or revoking roles for other users (F-1.6)
- Enforcing role-based access on operations (F-1.7)
- Inviting staff by email (F-1.8)
- Permission audit logging (F-1.9)
- Custom or user-defined roles
- Fine-grained per-field permissions
- Role-based UI rendering (showing/hiding buttons based on role) — belongs to F-1.7

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the five permission areas be represented as an enum in the domain, or as discrete value objects? | ✅ **Value objects** — more extensible when new permission areas are added later without breaking the model. |
| 2 | When the Owner role is assigned automatically on event creation, should this be a domain event or a silent side effect? | ✅ **Silent side effect** of event creation — no separate domain event needed; it is part of the event creation invariant. |
| 3 | Should the `EventUserRole` table include a `CreatedAt` timestamp? | ✅ **Include `CreatedAt`** — low cost now, useful for future audit logging (F-1.9). |
