---
artifact_type: spec
artifact_version: 1
id: spec-20260616011034-register-organizer-account
title: Register an organizer account
slug: register-organizer-account
filename_template: 20260616011034-register-organizer-account.md
created_at: 2026-06-16T01:10:34+07:00
updated_at: 2026-06-16T01:20:00+07:00
status: draft
plan_ready: true
owner: product
tags: [spec, eventhub, organizer-accounts-identity]
feature_refs: [F-1.1]
ddd_refs: [BC-1, AGG-User, INV-1, INV-2, EVT-UserRegistered]
prd_refs: [DEC-3, DEC-4, QG-1, QG-6, QG-7, PER-O1, PER-O2]
tech_refs: [Tech §5, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 1
search_index:
  keywords:
    - organizer registration
    - create account
    - email password
    - display name
    - cookie session
    - identity
    - self-service signup
    - password policy
    - duplicate email
  bounded_contexts: [BC-1 Identity & Access]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: [#1](https://github.com/tranvuongduy2003/eventhub/issues/1)

# Feature: Register an organizer account

> Features: F-1.1  |  Status: DRAFT  |  Date: 2026-06-16
> PRD: DEC-3 (MVP spine), DEC-4 (self-service)  |  DDD: BC-1 · AGG-User · INV-1, INV-2  |  Tech: §5–7 (session auth, PostgreSQL)

## 1. Problem & Solution

**Problem:** EventHub cannot attach ownership to events, authorize organizer actions, or run check-in until a person has a durable organizer identity. Without self-service registration, the platform cannot start the MVP spine (EP-1 → EP-2 → …).

**Solution:** A prospective organizer opens a public registration flow, submits email, password, and display name, and receives a new organizer account. On success they are immediately signed in via a browser session so they can proceed to create events without a separate sign-in step.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group / club organizer) — both need a fast, trustworthy way to create an account with no sales or admin intervention (DEC-4).

**Scope:**
- **In:** F-1.1 only — account creation, validation, persistence, automatic sign-in after success.
- **Out:** F-1.2 sign-in/sign-out as standalone flows (session establishment on register is in scope); F-1.3 profile edits; F-1.4 attendee accounts; email verification; social/OAuth login; password reset; admin-provisioned accounts; CAPTCHA/bot mitigation beyond basic validation.

## 2. Acceptance Criteria

**AC-01:** GIVEN I am not signed in and I provide a well-formed email not already registered, a password that meets the password rules, a valid display name, and matching password confirmation on the registration form, WHEN I submit registration, THEN a new organizer account is created, I receive a success response including my account identifier and display name, a session is started, and I am taken to the organizer home area.

**AC-02:** GIVEN I am not signed in and I provide an email that is already registered (after normalization), WHEN I submit registration, THEN no new account is created, I receive a clear message that the email is already in use (field-level on the form), and I remain unsigned in.

**AC-03:** GIVEN I provide a password that fails one or more password rules, WHEN I submit registration, THEN no account is created, I receive a clear message explaining which rule(s) failed (field-level on the password field), and I remain unsigned in.

**AC-04:** GIVEN I provide an invalid or empty display name (see §3), WHEN I submit registration, THEN no account is created, I receive a clear validation message on the display name field, and I remain unsigned in.

**AC-05:** GIVEN I provide an invalid or empty email, WHEN I submit registration, THEN no account is created, I receive a clear validation message on the email field, and I remain unsigned in.

**AC-06:** GIVEN I provide a valid password and confirmation that do not match, WHEN I submit registration on the client, THEN the form blocks submission with a clear mismatch message before any server request is sent.

**AC-07:** GIVEN registration succeeds, WHEN I inspect stored account data, THEN my password is never stored in plain text — only a one-way password hash is persisted (INV-2).

**AC-08:** GIVEN registration succeeds, WHEN a downstream system records the fact, THEN a user-registered domain event is emitted exactly once for the new account (EVT-UserRegistered).

**AC-09:** GIVEN I am already signed in as another user, WHEN I open the registration page, THEN I am redirected away to the organizer home area (registration is for anonymous visitors only).

**AC-10:** GIVEN a transient server failure during registration, WHEN the request fails, THEN I see a generic retry message, no partial account is left in an inconsistent state, and I can try again.

## 3. Domain & Business Rules

Align with BC-1 Identity & Access and AGG-User:

| Rule | Detail |
|------|--------|
| **INV-1** | Email addresses are unique across all accounts. Comparison uses normalized form (trim whitespace; treat email as case-insensitive for the local-part domain rules per standard email normalization). |
| **INV-2** | Passwords are stored only as a secure one-way hash; plain-text passwords never appear in logs, responses, or persistence. |
| **Role** | Accounts created through this flow have the **Organizer** role. Attendee role linking is out of scope (F-1.4). |
| **Display name** | Required public label for the organizer (maps to display name in product copy). Trimmed; length 1–64 characters after trim; must not be only whitespace. **Not** required to be unique — multiple accounts may share the same display name. |
| **Email** | Must be well-formed per standard email rules; max 254 characters. |
| **Password rules** | Minimum 8 characters; at least one letter (A–Z or a–z); at least one digit (0–9); at least one special character from `!@#$%^&*()_+-=[]{}|;:'",.<>?/\`~`. |
| **Behavior** | Registration is atomic: validate → create user aggregate → persist → start session → return success. Duplicate email is a business rejection, not a server error. |
| **Event** | On successful creation, emit EVT-UserRegistered with the new user identity and timestamp. |

Password confirmation is a **UI/input safeguard** only; it is not a persisted attribute.

## 4. UI Behavior

### Registration page (public, unauthenticated)

- Route reachable from the marketing/auth shell (e.g. link from login page: “Create account”).
- Mobile-first layout (QG-4): single-column form, full-width primary button, labels on all fields (QG-7).
- Fields:
  1. **Display name** — text input, autocomplete `name`.
  2. **Email** — email input, autocomplete `email`.
  3. **Password** — masked input, autocomplete `new-password`; optional short hint listing password rules.
  4. **Confirm password** — masked input, autocomplete `new-password`.
- Primary action: **Create account** (disabled while submitting; show loading state).
- Secondary link: **Already have an account? Log in** → sign-in page (F-1.2).
- Client-side validation mirrors server rules where practical (Zod-equivalent rules) so users get immediate feedback on blur/change.
- Server validation errors map to the relevant field; duplicate email maps to the email field; unexpected failures show a single root-level message.
- On success: update client session state, cache current-user query data, navigate to organizer home with replace (no back-stack to empty form).
- On error: clear password and confirm-password fields before showing errors.

### API contract (product level)

| Operation | Method & path | Success | Failure |
|-----------|---------------|---------|---------|
| Register organizer | `POST /api/users` | `201 Created` — body includes user id, display name, email, created timestamp; `Set-Cookie` session cookie | `400` malformed JSON; `422` validation or business rule (duplicate email, weak password) with RFC 7807 problem details including stable `code` and field errors |

After `201`, the browser holds a session cookie usable for `GET /api/auth/me` (same shape as post-login user summary). Registration does **not** require a separate login call.

## 5. Data & Storage Impact

| Store | Impact |
|-------|--------|
| **PostgreSQL** | New row in authoritative user table: stable user id, normalized email (unique index), display name (no uniqueness constraint), password hash, organizer role, created timestamp. |
| **Redis** | Session record written after successful commit (rebuildable cache; not source of truth). |
| **MinIO** | None for this slice. |
| **RabbitMQ** | None required for MVP registration; EVT-UserRegistered may be handled in-process initially. |

## 6. Real-Time & Consistency

N/A — registration is a synchronous request/response flow. No SignalR or integration-event fan-out is required for MVP beyond optional in-process domain-event handling.

## 7. Security & Privacy

- **Session:** HttpOnly, secure-in-production session cookie issued only after successful registration (same mechanism as sign-in per Tech §7).
- **Privacy (QG-6):** Collect only email, display name, and password — minimum needed for organizer identity. No marketing opt-in or phone number in this slice.
- **Enumeration:** Duplicate-email response may state that the email is taken (required by F-1.1 AC). Do not reveal whether a display name exists independently.
- **Transport:** HTTPS in non-local deployments; credentials only over TLS.
- **Rate limiting:** Desirable but not a hard MVP gate; document as follow-up if absent.

## 8. Edge Cases

**EC-01:** User submits registration twice quickly with the same email — second request fails with email taken; at most one account exists.

**EC-02:** Email entered with different casing or surrounding spaces — normalized before uniqueness check so `User@Example.com` and `user@example.com` conflict.

**EC-03:** User closes the tab mid-submit — if the server already committed, the account exists; user can sign in (F-1.2). If not committed, no account.

**EC-04:** Password meets length but fails complexity rules — specific rule messages shown, not a generic “invalid password.”

**EC-05:** Display name contains leading/trailing spaces — trimmed before save; inner spaces allowed.

**EC-06:** Unicode in display name — allowed if within length after trim; emoji allowed unless they violate length (product copy may discourage but need not block).

**EC-07:** Registration succeeds but cookie blocked by browser — rare; user must sign in manually (F-1.2); account still created.

## 9. Dependencies & Risks

| Type | Item |
|------|------|
| **Upstream** | None — F-1.1 is the foundation of EP-1. |
| **Downstream** | F-1.2 (returning users), F-2.1 (create draft event) depend on accounts existing. |
| **Risks** | Weak password policy vs usability (mitigated by clear inline rules); duplicate-email UX vs enumeration (accepted per F-1.1); scope creep into email verification or attendee registration (defer to F-1.4). |

## 10. Assumptions

- “Display name” in product copy is the organizer-facing label; it may be labeled “Username” in the UI if that matches existing auth screens, provided behavior matches §3.
- Self-service registration is open to anyone — no invite codes (DEC-4).
- Email verification is **not** required before using the account in MVP.
- **Email is the only unique identity field**; display names may duplicate across accounts (resolved 2026-06-16).
- **No terms-of-service or privacy-policy checkbox** at registration for MVP (resolved 2026-06-16).
- **No welcome email** on registration; notifications are deferred to a later epic (resolved 2026-06-16).
- Session duration and cookie name follow application configuration (Session section).
- Organizer home after registration is the same destination used after successful sign-in until a dedicated onboarding flow exists.

## 11. Out of Scope

- F-1.2 standalone sign-in and sign-out flows (except session cookie issued on register).
- F-1.3 profile management (avatar, email change).
- F-1.4 optional attendee account registration.
- Email verification / confirmation links.
- OAuth / social login.
- Forgot-password / reset-password.
- Admin or bulk user import.
- Welcome / confirmation email on registration (deferred to notifications epic).
- Terms-of-service or privacy-policy acceptance checkboxes (confirmed out of scope for MVP).

- CAPTCHA, device fingerprinting, or advanced bot defense.

## 12. Resolved decisions

| # | Question | Decision | Date |
|---|----------|----------|------|
| 1 | Should display names be globally unique, or only email? | **Email unique only; display name may duplicate.** | 2026-06-16 |
| 2 | Is a terms-of-service checkbox required at registration? | **No** for MVP. | 2026-06-16 |
| 3 | Should successful registration send a welcome email? | **No** — deferred to the notifications epic. | 2026-06-16 |
