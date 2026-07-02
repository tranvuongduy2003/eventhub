---
artifact_type: spec
artifact_version: 1
id: spec-20260620230000-invite-staff-to-event
title: Invite staff to an event
slug: invite-staff-to-event
filename_template: 20260620230000-invite-staff-to-event.md
created_at: "2026-06-20T23:00:00Z"
updated_at: "2026-06-20T23:00:00Z"
status: draft
owner: product
tags: [spec, eventhub, organizer-accounts]
feature_refs: [F-1.8]
ddd_refs: [BC-1, BC-6, AGG-User, AGG-Event]
prd_refs: [DEC-3, QG-1, QG-5, QG-6]
tech_refs: [Tech §4, Tech §5, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 19
search_index:
  keywords: [invite, staff, email, invitation, accept, revoke, expire, event, team, onboarding]
  bounded_contexts: [Identity and Access, Notifications]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #19 (https://github.com/tranvuongduy2003/eventhub/issues/19)

# Feature: Invite staff to an event

> Features: F-1.8  |  Status: DRAFT  |  Date: 2026-06-20
> PRD: DEC-3 (Next scope), QG-1 (simplicity), QG-5 (correct at small scale), QG-6 (responsible with data)
> DDD: BC-1 (Identity & Access), BC-6 (Notifications), AGG-User, AGG-Event
> Tech: §4 (CQRS pipeline), §5 (messaging/email), §6 (persistence), §7 (API conventions)

## 1. Problem & Solution

**Problem:** F-1.6 lets an event Owner assign roles to users who already have accounts, but there is no way to invite someone who does not yet have an account, or to send a formal invitation that the recipient can accept or decline. In practice, the organizer must tell the person to register first, then manually assign them — a multi-step, error-prone process. There is also no time-bound mechanism: a role assignment is immediate and permanent until revoked, with no concept of an invitation that can expire.

**Solution:** Let an event Owner invite a person by email to become Staff on an event. The system sends an invitation email with a unique, time-limited acceptance link. If the invitee already has an account, they accept and are assigned Staff immediately. If they do not have an account, they are prompted to register first, then the invitation is fulfilled automatically. The Owner can revoke an unaccepted invitation at any time. Invitations expire after a configurable window (default 7 days) and cannot be accepted after expiry.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group/club organizer)

**Scope:**
- **In:** Inviting a person by email to the Staff role. Invitation email delivery. Acceptance flow (with or without existing account). Revocation of pending invitations. Expiry of invitations. Listing pending invitations for an event.
- **Out:** Inviting for the Owner role (ownership transfer remains a direct assignment per F-1.6). Custom roles. Bulk invitations. Resending invitation emails. Audit logging (F-1.9).

## 2. Acceptance Criteria

**AC-01:** GIVEN I hold the Owner role for an event and the invitee has an existing account, WHEN I invite that person by email with the Staff role, THEN an invitation email is sent to that address containing a unique acceptance link, and the invitation is recorded as Pending.

**AC-02:** GIVEN a Pending invitation exists and the invitee has an existing account, WHEN the invitee opens the acceptance link and confirms, THEN the invitee is assigned the Staff role for that event and the invitation status becomes Accepted.

**AC-03:** GIVEN a Pending invitation exists and the invitee does NOT have an account, WHEN the invitee opens the acceptance link, THEN they are prompted to register (F-1.1). After successful registration, the invitation is automatically accepted and the new user is assigned the Staff role for that event.

**AC-04:** GIVEN I hold the Owner role for an event and a Pending invitation exists, WHEN I revoke that invitation, THEN its status becomes Revoked and the acceptance link no longer works. A revoked invitation cannot be accepted.

**AC-05:** GIVEN a Pending invitation exists, WHEN the configurable expiry window passes without acceptance, THEN the invitation status becomes Expired and the acceptance link no longer works. An expired invitation cannot be accepted.

**AC-06:** GIVEN a Pending invitation exists for an email that already has a role on the event (e.g., already Staff or Owner), WHEN the invitation is accepted, THEN the acceptance is rejected with a clear message that the user already has a role on this event.

**AC-07:** GIVEN I hold the Owner role for an event, WHEN I invite an email that already has a Pending invitation for that event, THEN the operation is rejected with a clear message that an invitation is already pending for that email.

**AC-08:** GIVEN I hold the Owner role for an event, WHEN I view the list of invitations for that event, THEN I see each invitation's email, status (Pending/Accepted/Revoked/Expired), created date, and expiry date.

**AC-09:** GIVEN I do NOT hold the Owner role for an event, WHEN I try to send or revoke an invitation for that event, THEN the operation is rejected — only the Owner can manage invitations.

**AC-10:** GIVEN a Pending invitation exists, WHEN the invitee is already a registered user and is currently signed in, THEN they can accept the invitation directly from the link without re-entering credentials.

## 3. Domain & Business Rules

Reference: `domain-model-specification.md` BC-1 (Identity & Access), BC-6 (Notifications). The invitation model introduces a new entity that bridges identity and event access.

**BR-01 — Invitation is per-event, per-email:** An invitation is a triple of (EventId, Email, Role). There can be at most one Pending invitation per (EventId, Email) at any time.

**BR-02 — Only Staff role is invitable:** Invitations are limited to the Staff role. Ownership transfer remains a direct assignment (F-1.6). This keeps the invitation flow simple and avoids accidental ownership transfers.

**BR-03 — Invitation has a lifecycle:** Pending → Accepted, Pending → Revoked, or Pending → Expired. Only Pending invitations can be accepted, revoked, or expired. Status transitions are one-way and irreversible.

**BR-04 — Expiry is configurable per-invitation:** The default expiry window is 7 days. The Owner may specify a different window when creating the invitation via `expiresInDays` (within system-imposed bounds: min 1 day, max 30 days).

**BR-05 — Acceptance is idempotent with respect to role assignment:** If the invitee already holds the Staff role on the event (assigned via F-1.6 before the invitation was sent), accepting the invitation is a no-op or is rejected — not a duplicate assignment.

**BR-06 — Invitation token is unguessable and hashed:** The acceptance link contains a cryptographically random token. The token is stored as a SHA-256 hash in the database; acceptance works by hashing the incoming token and comparing. The token is single-use: once the invitation is accepted, revoked, or expired, the token is invalidated.

**BR-07 — Registration-then-accept is atomic from the user's perspective:** When an invitee without an account registers via the invitation link, the registration and role assignment happen in sequence — the user sees themselves as Staff on the event immediately after completing registration.

**BR-08 — Invitation email is a side effect:** Sending the invitation email is an asynchronous side effect (via BC-6 Notifications / RabbitMQ). The invitation record is created first; if email delivery fails, the invitation still exists and can be retried.

## 4. UI Behavior **or** API Contract

**API endpoints (product-level contract):**

| Operation | Method / Path | Request | Response |
|-----------|---------------|---------|----------|
| Send invitation | `POST /api/events/{eventId}/invitations` | `{ "email": "alice@example.com", "expiresInDays": 7 }` (expiresInDays optional, default 7) | `201` — the created invitation (id, email, status, expiresAt) |
| Revoke invitation | `DELETE /api/events/{eventId}/invitations/{invitationId}` | — | `204` — no body |
| List invitations | `GET /api/events/{eventId}/invitations` | — | `200` — array of invitations with status and dates |
| Accept invitation | `POST /api/invitations/{invitationId}/accept` | `{ "token": "<token>" }` | `200` — confirmation of role assignment |

**Invitation email content:**
- Subject: "You've been invited to help run [Event Title]"
- Body: Organizer name, event title, event date, a clear CTA button/link to accept.
- The link points to a dedicated acceptance page that handles both registered and unregistered users.

**Acceptance page behavior:**
- If the user is signed in: show event details and an "Accept invitation" button.
- If the user is not signed in and has an account: prompt to sign in, then show the accept button.
- If the user has no account: redirect to registration with the invitation context preserved; after registration, auto-accept and redirect to the event.

**Error responses (RFC 7807):**
- Caller is not the Owner → `403` with code `INSUFFICIENT_PERMISSIONS`
- Email already has a Pending invitation → `409` with code `INVITATION_ALREADY_PENDING`
- Invitation not found → `404` with code `INVITATION_NOT_FOUND`
- Invitation is not Pending (expired/revoked/already accepted) → `422` with code `INVITATION_NOT_ACCEPTABLE`
- Invitee already has a role on the event → `409` with code `ROLE_ALREADY_ASSIGNED`

**UI (organizer-facing):**
- A "Team" section on the event management page (extending F-1.6's assignment list) with an "Invite by email" input field.
- Pending invitations appear alongside current role assignments, with a "Revoke" action.
- Status badges: Pending (with countdown to expiry), Accepted, Revoked, Expired.

## 5. Data & Storage Impact

**PostgreSQL (`app` schema):**

| Table | Columns | Notes |
|-------|---------|-------|
| `event_invitation` | `id` (PK, UUID), `event_id` (FK), `email` (text), `role` (enum), `token_hash` (text, unique), `status` (enum: Pending/Accepted/Revoked/Expired), `inviter_id` (FK to user), `created_at`, `expires_at`, `accepted_at?`, `revoked_at?` | New table for invitation lifecycle. Token stored as SHA-256 hash. |

- **Index:** `(event_id)` for listing invitations per event. `(token_hash)` unique index for acceptance link lookup. `(event_id, email, status)` for duplicate-pending check.
- **Unique constraint:** `(event_id, email)` where `status = 'Pending'` — at most one pending invitation per email per event. Partial unique index in PostgreSQL.
- **Enum:** `status` column uses a PostgreSQL enum or string check constraint with values `Pending`, `Accepted`, `Revoked`, `Expired`.

**Existing table impact:**
- `event_user_role` — no schema change. When an invitation is accepted, a row is inserted here (same as F-1.6 direct assignment).

**Redis:** No impact for this slice. Invitation tokens are looked up from PostgreSQL (low volume, no caching needed).

**MinIO:** No impact.

**RabbitMQ:** An integration event (`EVT-InvitationCreated`) is published when a new invitation is created, consumed by BC-6 (Notifications) to send the email. If email delivery fails, the invitation record persists and the message can be retried.

## 6. Real-Time & Consistency

**N/A** for this slice. Invitations are request-scoped operations; no SignalR push is required.

**Consistency:**
- Creating an invitation and publishing the email event: the invitation record is written in the current transaction; the RabbitMQ message is published after commit (standard integration event pattern per `technical-design.md` §4/§5).
- Accepting an invitation and assigning the role: both happen in the same transaction (the invitation status changes to Accepted and the `event_user_role` row is inserted in one unit of work).

## 7. Security & Privacy

**SEC-01 — Caller authorization:** Only the event Owner can send or revoke invitations. The handler must verify the caller holds the Owner role before executing.

**SEC-02 — Token security:** The invitation token must be cryptographically random (e.g., 32-byte GUID or base64-encoded random bytes). The token is stored as a SHA-256 hash in the database; acceptance works by hashing the incoming token and comparing against the stored hash.

**SEC-03 — Email verification:** The invitation is sent to the email specified by the Owner. There is no verification that the email belongs to the intended person — this is consistent with the simplicity principle (QG-1). The acceptance link is the proof of intent.

**SEC-04 — No privilege escalation:** Invitations are limited to the Staff role only. An Owner cannot invite someone as Owner — ownership transfer must go through F-1.6's direct assignment, which is intentional and explicit.

**SEC-05 — Rate limiting:** The send-invitation endpoint should be rate-limited per event to prevent spam (e.g., max 20 invitations per event per hour). This is an infrastructure concern, not a domain rule.

**SEC-06 — Guest access unaffected:** The invitation system is organizer-side. Public operations (viewing events, purchasing tickets) are not affected.

## 8. Edge Cases

**EC-01:** GIVEN an invitation is Pending for alice@example.com, WHEN the Owner sends another invitation to the same email for the same event, THEN the operation is rejected with `INVITATION_ALREADY_PENDING`.

**EC-02:** GIVEN an invitation is Pending, WHEN the invitee independently registers an account (not via the invitation link) and the Owner assigns them Staff via F-1.6, THEN the invitation can still be accepted but becomes a no-op (the user already has the role) — or the system rejects the acceptance with `ROLE_ALREADY_ASSIGNED`.

**EC-03:** GIVEN an invitation is Pending, WHEN the invitee's email is assigned the Owner role via F-1.6 (ownership transfer), THEN the invitation can still be accepted but the acceptance is rejected because the user already has a role (Owner) on the event.

**EC-04:** GIVEN an invitation has Expired, WHEN the invitee clicks the acceptance link, THEN they see a clear message that the invitation has expired and to contact the organizer for a new one.

**EC-05:** GIVEN an invitation has been Revoked, WHEN the invitee clicks the acceptance link, THEN they see a clear message that the invitation is no longer valid.

**EC-06:** GIVEN the Owner sends an invitation, WHEN the email address belongs to the Owner themselves, THEN the operation is rejected — you cannot invite yourself.

**EC-07:** GIVEN an invitation is Pending and the event is Cancelled, WHEN the invitee tries to accept, THEN the acceptance is rejected — invitations for cancelled events cannot be accepted.

**EC-08:** GIVEN an invitation email fails to deliver (bounce), WHEN the Owner checks the invitation list, THEN the invitation still shows as Pending — the Owner can revoke and re-send to a corrected email.

**EC-09:** GIVEN an invitation is Pending, WHEN the Owner who sent it loses their Owner role (someone else becomes Owner via F-1.6), THEN the invitation remains valid — it was created when the sender was authorized, and revocation requires the current Owner.

## 9. Dependencies & Risks

**Dependencies:**
- F-1.1 (Register an organizer account) — invitees without accounts must be able to register.
- F-1.5 (Define roles and permissions) — the Staff role and its permissions must be defined.
- F-1.6 (Assign roles to users per event) — acceptance creates a role assignment using the same mechanism.
- F-7.2 (Deliver tickets by email) — establishes the email delivery infrastructure (BC-6, `IEmailSender` via RabbitMQ) that invitation emails reuse.

**Risks:**
- **RSK-I1 — Email delivery reliability:** If the email provider is down or the email bounces, the invitation is created but the invitee never receives it. Mitigation: the invitation list shows Pending status so the Owner can see it was not accepted; retry logic on the RabbitMQ consumer.
- **RSK-I2 — Token leakage:** If an invitation link is forwarded or leaked, anyone with the link can accept it (if they can register with that email). Mitigation: the token is single-use and time-limited; the email is fixed at creation.
- **RSK-I3 — Stale invitations:** Owners may forget to revoke old invitations. Mitigation: automatic expiry handles this; the default 7-day window is short enough to limit exposure.

## 10. Assumptions

- **ASM-I1:** The invitation system reuses the existing email delivery infrastructure from F-7.2 (RabbitMQ → Notifications BC → `IEmailSender`). If F-7.2's email pipeline is not yet available, this feature cannot be fully completed.
- **ASM-I2:** Invitations are limited to the Staff role only. The Owner role is never assigned via invitation — always via direct assignment (F-1.6).
- **ASM-I3:** The invitation acceptance page is a simple web page served by the frontend (web/). It is not a deep link into a mobile app.
- **ASM-I4:** There is no concept of "invitation groups" or "bulk invitations." Each invitation is for one email address.
- **ASM-I5:** An invitation does not prevent the invitee from being assigned a role directly via F-1.6. The two mechanisms coexist — direct assignment is immediate, invitation is email-based and time-bound.

## 11. Out of Scope

- Inviting for the Owner role (ownership transfer remains F-1.6)
- Bulk invitations (multiple emails at once)
- Resending invitation emails
- Custom roles (only Staff is invitable)
- Permission audit logging (F-1.9)
- Attendee-facing invitations (attendees do not hold roles)
- Invitation templates or customizable email content
- Deep link from email into a native mobile app

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the invitation acceptance page live at a dedicated URL (e.g., `/invitations/:id/accept?token=...`) or be integrated into the existing auth flow? | ✅ Dedicated URL — cleaner for the email link and handles the register-then-accept flow naturally. |
| 2 | Should the Owner be able to set a custom expiry window when sending an invitation, or is the 7-day default sufficient for all cases? | ✅ Configurable — default 7 days, optional override with min/max bounds. |
| 3 | If an invitation expires, should the system send a notification to the Owner? | ✅ Out of scope for this slice. The Owner can see expired invitations in the list. |
| 4 | Should the `event_invitation` table store the token in plaintext or as a hash? | ✅ Implement hash — store a SHA-256 hash of the token; accept by hashing the incoming token and comparing. |
| 5 | When an invitee without an account registers via the invitation link, should the registration happen on a special "invitation registration" page or the standard registration page with the invitation context passed through? | ✅ The standard registration page with context (invitation ID + token passed as query params). |
