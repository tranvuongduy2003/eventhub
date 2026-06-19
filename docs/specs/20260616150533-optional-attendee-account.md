---
artifact_type: spec
artifact_version: 1
id: spec-20260616150533-optional-attendee-account
title: Optional attendee account
slug: optional-attendee-account
filename_template: 20260616150533-optional-attendee-account.md
created_at: "2026-06-16T15:05:33Z"
updated_at: "2026-06-16T15:05:33Z"
status: draft
owner: product
tags: [spec, eventhub, ep-1-organizer-accounts]
feature_refs: [F-1.4]
ddd_refs: [BC-1, AGG-User, VO-UserId, VO-EmailAddress, VO-UserRole, INV-1]
prd_refs: [DEC-3, QG-1, QG-6]
tech_refs: [Tech §4, Tech §5, Tech §7]
db_refs: [Tech §6]
github_issue: null
search_index:
  keywords: [attendee, account, optional, guest, tickets, link, email, sign-up, register]
  bounded_contexts: [Identity & Access]
  user_personas: [PER-A1, PER-A2]
---

# Feature: Optional attendee account

> Features: F-1.4  |  Status: DRAFT  |  Date: 2026-06-16
> PRD: DEC-3, QG-1, QG-6  |  DDD: BC-1, AGG-User  |  Tech: §4, §5, §7

## 1. Problem & Solution

**Problem:** Attendees who buy tickets as guests (F-5.2) have no way to see all their tickets in one place. Each purchase is isolated by email; there is no persistent identity that ties past orders together. When F-7.6 (attendee ticket wallet) is built, it needs an account to anchor the consolidated view.

**Solution:** Allow an attendee to optionally create an account using their email and a password. The account links to any existing orders that share the same email address, so past purchases are not lost. Guest checkout remains the default — buying never requires an account. The account is a lightweight identity for the attendee persona, not a prerequisite for any purchase flow.

**Personas:** PER-A1 (general attendee), PER-A2 (group buyer)

**Scope:**
- **In:** F-1.4 — attendee account registration, email-based linking of past orders/tickets, attendee role on the User aggregate
- **Out:** F-7.6 (attendee ticket wallet — the UI that displays linked tickets), F-1.3 (organizer profile management), password reset flow, attendee profile editing beyond registration

## 2. Acceptance Criteria

**AC-01:** GIVEN a visitor with no account, WHEN they provide a valid email, a display name, and a password that meets the rules, THEN an attendee account is created and they are signed in.

**AC-02:** GIVEN the email is already in use (by an organizer or another attendee), WHEN an attendee tries to register, THEN they are told the email is taken and no account is created.

**AC-03:** GIVEN a password that fails the rules (too short, too common, etc.), WHEN an attendee tries to register, THEN they are told why and no account is created.

**AC-04:** GIVEN an attendee account with email `alice@example.com`, WHEN the account is created and orders exist in the system with the same email (`alice@example.com` as guest buyer), THEN those orders are linked to the attendee account so they can be retrieved later (by F-7.6).

**AC-05:** GIVEN a visitor with no account, WHEN they buy tickets as a guest (F-5.2), THEN the purchase completes normally without requiring account creation. Guest checkout is unaffected.

**AC-06:** GIVEN an attendee with an account, WHEN they sign in, THEN they have an active session and can access attendee-specific areas (e.g., the future ticket wallet F-7.6).

**AC-07:** GIVEN an attendee with an account, WHEN they sign out, THEN their session ends and protected areas are no longer accessible.

**AC-08:** GIVEN an attendee registers with an email that matches a past guest order, WHEN they view their account after sign-in, THEN the linked orders/tickets from that email are associated with their account (visible once F-7.6 is built).

**AC-09:** GIVEN an organizer already exists with email `bob@example.com`, WHEN someone tries to register an attendee account with the same email, THEN they are rejected — one email maps to one account, regardless of role.

## 3. Domain & Business Rules

**Bounded context:** BC-1 (Identity & Access)

**Aggregate:** AGG-User

**Value objects involved:**
- `VO-UserId` — identity (already exists)
- `VO-EmailAddress` — well-formed, normalized, unique across all accounts (already exists)
- `VO-DisplayName` — 1–64 characters, trimmed (already exists)
- `VO-PasswordHash` — password stored only as hash (already exists, INV-2)
- `VO-UserRole` — `Organizer` or `Attendee` (already defined in ddd.md)

**Invariants:**
- `INV-1` — Email must be unique across all users. An attendee cannot register with an email already held by an organizer or another attendee.
- `INV-2` — A password is only ever stored as a hash.
- Email must be well-formed (VO-EmailAddress rules).

**Behavior:**
- `Register` (existing) is extended or a parallel `RegisterAttendee` factory method creates a User with role `Attendee`.
- `LinkAttendeeIdentity` (defined in ddd.md as "optional, Later — F-1.4") — on account creation, the system links existing orders/tickets that share the same email address. This is an Application-layer orchestration (query orders by email, associate with the new UserId), not a domain invariant on the User aggregate itself.

**Domain event:** `EVT-UserRegistered` (already exists) — scope and consumers remain the same.

**Key design principle:** The attendee account is an **identity anchor**, not a gate. Guest checkout (F-5.2) is untouched. The account exists so that F-7.6 (ticket wallet) can later display consolidated tickets.

## 4. API Surface (high-level)

> Detailed endpoint design is deferred to the engineering plan (`/plan`).

- **Attendee registration** is a public endpoint (no auth). Auto sign-in on success.
- **Sign-in / sign-out** reuses the existing session mechanism (`POST /api/auth/login`, `POST /api/auth/logout`).
- **Current user** (`GET /api/auth/me`) includes the `role` field so the frontend distinguishes organizer vs attendee.
- **Order linking** is server-side only — happens automatically at registration time. No dedicated endpoint.
- Attendee endpoints use a **separate route prefix** from organizer endpoints.

## 5. Data & Storage Impact

**PostgreSQL (app schema):**
- `users` table: add `role` column to distinguish `Organizer` from `Attendee`. The aggregate is currently a template; this is a greenfield addition, not a backfill migration.
- `orders` table (BC-3): no schema change. Orders already store `Contact.Email`. The linking is done by matching email at registration time and setting a `buyer_user_id` (or equivalent) on matched orders — this requires a nullable column if not already present.
- Migration: append-only; adds role column and nullable `buyer_user_id` on orders (if linking is materialized).

**Redis:** Session cache works for attendee sessions identically to organizer sessions. No new cache concerns.

**MinIO:** No changes. Attendee accounts have no avatar in this feature (avatar is part of F-1.3 for organizers; attendee avatar is out of scope).

**RabbitMQ:** No changes. `EVT-UserRegistered` is already an integration event; consumers handle it regardless of role.

## 6. Real-Time & Consistency

**N/A for this feature.** Attendee account creation does not require real-time push. The linking of past orders happens at registration time in the same request; it is a one-time operation, not a streaming concern.

Consistency is strong within the User aggregate (single transaction for account creation). Order linking is a best-effort Application-layer operation within the same unit of work — if linking fails partially, the account is still created (the attendee can see their future orders; past orders can be linked later if needed).

## 7. Security & Privacy

**Session required for attendee areas:** After registration, the attendee has a session identical in mechanism to an organizer session. The role field governs access.

**Email uniqueness (INV-1):** Prevents duplicate accounts. One email → one account, regardless of role.

**Password rules (INV-2):** Same rules as organizer registration (F-1.1). Password is hashed; never stored in plaintext.

**Guest checkout preserved (QG-6):** No personal data is required beyond what F-5.2 already collects (name + email). The attendee account is additive, not a data-collection expansion.

**No payment boundary involvement (DEC-1).** Attendee accounts do not change payment flows.

**Linking privacy:** Only orders matching the exact email are linked. No cross-email inference. The attendee can only see orders associated with their own email.

## 8. Edge Cases

**EC-01:** An attendee registers, then later an organizer tries to register with the same email. → Rejected with "email taken" (INV-1). One email, one account.

**EC-02:** An attendee registers with an email that has many past guest orders (e.g., a prolific buyer). → All matching orders are linked. No limit; the linking is a set-based update.

**EC-03:** An attendee registers, buys tickets as a guest with a *different* email (e.g., typo or alternate email). → Those orders are not linked. Only exact email matches are linked. The attendee would need to use the same email for future purchases to have them appear in the wallet (F-7.6).

**EC-04:** An attendee registers and immediately signs out before any linking completes. → If linking is in the same transaction, it completes before the response. If async, it completes shortly after. Either way, the attendee sees linked orders on next sign-in.

**EC-05:** An organizer who already has an account tries to also register as an attendee with the same email. → Rejected. The account already exists. If the organizer wants an attendee identity, they use their existing account (role could be extended later, but is out of scope for this feature).

**EC-06:** Network failure during registration after account creation but before linking response. → The account exists (committed). Past orders may or may not be linked. The attendee can sign in; linking can be retried or handled as a background job.

**EC-07:** Attendee registers with an email that has a pending (unconfirmed) order. → The pending order is linked the same as confirmed orders. When it confirms or expires, the link persists.

## 9. Dependencies & Risks

**Dependencies:**
- F-1.1 (register organizer account) — the User aggregate, registration flow, and session mechanism are already built. ✅ Done.
- F-5.2 (guest checkout) — must exist so that "guest checkout stays" is verifiable. Assumed available by the time F-1.4 is built (MVP phase).

**Downstream features:**
- F-7.6 (attendee ticket wallet) — depends on F-1.4. This feature creates the account; F-7.6 builds the UI to display linked tickets.

**Risks:**
- **Role design.** The existing User aggregate may not have a role field. Adding one is a schema change that affects existing organizer accounts (backfill). Low risk but requires a migration.
- **Linking scope.** Linking past orders by email is a one-time batch operation at registration. If the orders table is large, the query could be slow. At this project's scale (small events, ASM-2), this is negligible.
- **Scope creep into F-7.6.** The temptation to build a minimal ticket list alongside registration is high. This spec intentionally excludes it — F-1.4 creates the identity; F-7.6 displays the tickets.

## 10. Assumptions

1. The User aggregate can hold both organizer and attendee roles (via `VO-UserRole` as defined in ddd.md). The current aggregate is a template; the role field is added as part of this feature — no migration concern.
2. Guest orders store the buyer's email in a queryable field (`Contact.Email` on the Order aggregate). This is already the case per ddd.md BC-3.
3. Account registration reuses the same password rules as F-1.1 (organizer registration). No separate attendee-specific password policy.
4. The session mechanism (cookie + Redis) works identically for both roles. No session-level distinction beyond the user's role field.
5. Order linking is synchronous at registration time (within the same request). If performance becomes a concern, it can be moved to a background job, but at this scale it is unnecessary.
6. Display name for attendees follows the same rules as organizers (1–64 chars, trimmed, non-unique).

## 11. Out of Scope

- **F-7.6 — Attendee ticket wallet** (the UI that shows consolidated tickets). This feature only creates the account and links orders; the wallet is a separate feature.
- **Password reset / forgot password** — a cross-cutting auth concern, not specific to attendee accounts.
- **Attendee profile editing** (change display name, email, avatar after registration) — could be a future feature; not part of F-1.4.
- **Merging multiple guest emails into one account** — only exact email matches are linked. No email aliasing or merging.
- **Attendee-specific role-based access control** beyond distinguishing attendee vs organizer areas.
- **Social login / OAuth** — not in scope for any persona.

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | ~~Should attendee registration use the same endpoint as organizers or a separate one?~~ | Deferred to `/plan` |
| 2 | Should order linking be synchronous at registration time, or can it be deferred to a background job? | ✅ Resolved — synchronous, in the same unit of work as account creation. Simpler; sufficient at this scale. |
| 3 | Does the existing User aggregate already support a role field, or does it need to be added? | ✅ Resolved — `VO-UserRole` (`Organizer` / `Attendee`) is defined in `ddd.md` BC-1. The current aggregate is a template; the role field will be added as part of this feature. No migration concern. |
