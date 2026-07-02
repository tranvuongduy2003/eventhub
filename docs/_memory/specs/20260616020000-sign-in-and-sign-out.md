---
artifact_type: spec
artifact_version: 1
id: spec-20260616020000-sign-in-and-sign-out
title: Sign in and sign out
slug: sign-in-and-sign-out
filename_template: 20260616020000-sign-in-and-sign-out.md
created_at: 2026-06-16T02:00:00+07:00
updated_at: 2026-06-16T02:00:00+07:00
status: draft
owner: product
tags: [spec, eventhub, organizer-accounts-identity]
feature_refs: [F-1.2]
ddd_refs: [BC-1, AGG-User, AGG-Session, INV-3, EVT-UserRegistered]
prd_refs: [DEC-3, DEC-4, QG-1, QG-6, QG-7, PER-O1, PER-O2]
tech_refs: [Tech §5, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 6
search_index:
  keywords:
    - sign in
    - sign out
    - login
    - logout
    - session
    - authentication
    - credentials
    - cookie
    - organizer access
  bounded_contexts: [BC-1 Identity & Access]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: [#6](https://github.com/tranvuongduy2003/eventhub/issues/6)

# Feature: Sign in and sign out

> Features: F-1.2  |  Status: DRAFT  |  Date: 2026-06-16
> PRD: DEC-3 (MVP spine)  |  DDD: BC-1 · AGG-Session · INV-3  |  Tech: §5–7 (session auth, PostgreSQL)

## 1. Problem & Solution

**Problem:** Once an organizer has an account (F-1.1), they need a way to return to their events and organizer tools across browser sessions. Without sign-in, every visit requires re-registration; without sign-out, sessions stay open on shared devices.

**Solution:** An organizer enters their email and password on a sign-in form. On success, a server session is created and a session cookie is issued, granting access to protected organizer areas. Signing out destroys the session and clears the cookie, ending access immediately.

**Personas:** PER-O1 (individual organizer), PER-O2 (small group / club organizer) — both need to return to their events securely.

**Scope:**
- **In:** F-1.2 only — sign-in form, credential validation, session creation, sign-out, session expiry behavior.
- **Out:** F-1.1 registration (already complete); F-1.3 profile management; F-1.4 attendee accounts; forgot-password / reset-password; OAuth / social login; multi-device session management; remember-me / persistent sessions beyond the configured session window.

## 2. Acceptance Criteria

**AC-01:** GIVEN I am not signed in and I provide a registered email and its correct password on the sign-in form, WHEN I submit sign-in, THEN a session is created, a session cookie is issued, and I am taken to the organizer home area.

**AC-02:** GIVEN I am not signed in and I provide an email that does not exist or a password that does not match the stored hash, WHEN I submit sign-in, THEN I am refused with a clear message that the credentials are invalid (without revealing whether the email or the password was wrong), no session is created, and I remain unsigned in.

**AC-03:** GIVEN I am not signed in and I provide an empty or malformed email, WHEN I attempt to submit sign-in, THEN the form blocks submission with a clear validation message on the email field.

**AC-04:** GIVEN I am not signed in and I provide an empty password, WHEN I attempt to submit sign-in, THEN the form blocks submission with a clear validation message on the password field.

**AC-05:** GIVEN I have an active session, WHEN I open a protected organizer area, THEN I am granted access without re-entering credentials.

**AC-06:** GIVEN I have an active session, WHEN I choose to sign out, THEN my session is destroyed, the session cookie is cleared, I am redirected to the sign-in page (or public landing), and subsequent requests to protected areas are denied.

**AC-07:** GIVEN I have an active session that has exceeded the configured absolute session duration (from sign-in), WHEN I next make a request, THEN the session is treated as expired, I am redirected to sign in, and no protected data is returned.

**AC-08:** GIVEN I am already signed in, WHEN I open the sign-in page, THEN I am redirected to the organizer home area (sign-in is for anonymous visitors only).

**AC-09:** GIVEN I am not signed in and I provide valid credentials, WHEN sign-in succeeds, THEN the session cookie is HttpOnly and has the secure flag set in non-local environments (QG-6).

**AC-10:** GIVEN a transient server failure during sign-in, WHEN the request fails, THEN I see a generic retry message, no session is partially created, and I can try again.

**AC-11:** GIVEN I successfully sign in, WHEN I navigate to any page that requires authentication, THEN I see my identity (display name) available to the application for ownership and authorization checks.

**AC-12:** GIVEN I have active sessions on multiple devices, WHEN my password is changed (via a future password-change flow), THEN all existing sessions are invalidated and I must sign in again with the new password on each device.

## 3. Domain & Business Rules

Align with BC-1 Identity & Access, AGG-User, and AGG-Session:

| Rule | Detail |
|------|--------|
| **Credential validation** | Email is looked up after normalization (same normalization as F-1.1). The submitted password is compared against the stored hash using the same hashing algorithm used at registration. |
| **No field disclosure** | A failed sign-in response must not indicate whether the email was unknown or the password was wrong. This prevents account enumeration. |
| **INV-3** | An expired session grants no access. Session expiry is checked on each authenticated request; expired sessions are treated as absent. |
| **Session lifecycle** | A session starts (`Start` behavior on AGG-Session) on successful sign-in and is destroyed (`Invalidate` behavior) on sign-out or expiry. Expired sessions are cleaned up automatically by a background process or TTL-based eviction. |
| **Session invalidation on password change** | When an organizer changes their password (future feature), all existing sessions for that user are immediately invalidated. The user must sign in again with the new password. This prevents stale sessions from remaining active after a credential change. |
| **Session duration** | Driven by application configuration (not user-configurable). Fixed **absolute** timeout from sign-in (e.g., 24 hours from authentication, not reset on activity). |
| **Concurrent sessions** | Signing in from a second device does not invalidate the first session unless configured otherwise. Multiple active sessions per user are permitted for MVP simplicity (QG-1). |
| **Lockout** | No account lockout after failed attempts for MVP. Rate limiting at the transport layer is desirable but not a hard gate. |

## 4. UI Behavior

### Sign-in page (public, unauthenticated)

- Route reachable from the marketing/auth shell (e.g. link from registration page: "Already have an account? Log in").
- Mobile-first layout (QG-4): single-column form, full-width primary button, labels on all fields (QG-7).
- Fields:
  1. **Email** — email input, autocomplete `email`.
  2. **Password** — masked input, autocomplete `current-password`.
- Primary action: **Log in** (disabled while submitting; show loading state).
- Secondary link: **Don't have an account? Create one** → registration page (F-1.1).
- Client-side validation: email format and required-field checks on blur; password required check on submit.
- Server validation errors (invalid credentials) show a single root-level message — not per-field, to avoid leaking which field was wrong.
- On success: update client session state, cache current-user query data, navigate to organizer home with replace (no back-stack to sign-in form).
- On error: clear the password field before showing the error message.

### Sign-out action

- Accessible from a persistent UI element in the authenticated shell (e.g. user menu, sidebar footer).
- Action: **Sign out** — triggers a server call to destroy the session, clears local client state, and navigates to the sign-in page.
- No confirmation dialog for MVP (QG-1); sign-out is a low-risk, easily reversible action.

### API contract (product level)

| Operation | Method & path | Success | Failure |
|-----------|---------------|---------|---------|
| Sign in | `POST /api/auth/login` | `200 OK` — body includes user id, display name, email; `Set-Cookie` session cookie | `401 Unauthorized` — body is RFC 7807 problem details with a generic `invalid_credentials` code |
| Sign out | `POST /api/auth/logout` | `204 No Content` — session destroyed, cookie cleared | `204` even if no session existed (idempotent) |
| Current user | `GET /api/auth/me` | `200 OK` — user id, display name, email, role | `401 Unauthorized` — no valid session |

After `200` on login, the browser holds a session cookie usable for `GET /api/auth/me` and all authenticated endpoints.

## 5. Data & Storage Impact

| Store | Impact |
|-------|--------|
| **PostgreSQL** | No schema changes. Sessions may be stored server-side (PostgreSQL or Redis) per Tech §7 configuration. The user table is read-only for this slice (credential verification). |
| **Redis** | If sessions are stored in Redis: session record created on sign-in, deleted on sign-out, auto-expires per TTL. Redis is rebuildable; the authoritative user record stays in PostgreSQL. |
| **MinIO** | None. |
| **RabbitMQ** | None. Sign-in/sign-out are synchronous request/response flows with no integration events. |

## 6. Real-Time & Consistency

N/A — sign-in and sign-out are synchronous request/response flows. No SignalR or integration-event fan-out is required.

Session state is immediately consistent: the cookie is set or cleared in the same HTTP response, and subsequent requests reflect the new state.

## 7. Security & Privacy

- **Session cookie:** HttpOnly, Secure flag in non-local deployments, SameSite=Lax or Strict per Tech §7. The cookie value is an opaque session identifier, not a JWT containing user data.
- **Credential transport:** Email and password are sent only over HTTPS in non-local environments.
- **Enumeration prevention (QG-6):** The sign-in error message is generic ("Invalid email or password") — never "email not found" or "wrong password."
- **No credential storage in client:** Passwords are never stored in localStorage, sessionStorage, or client-side cookies.
- **Sign-out completeness:** Sign-out destroys the server-side session and clears the cookie. A stale cookie after sign-out must not grant access.
- **Session fixation:** A new session identifier is generated on each sign-in; the old identifier (if any) is invalidated.

## 8. Edge Cases

**EC-01:** User submits sign-in twice quickly with valid credentials — two requests may create two sessions; both are valid until expiry. No harm at MVP scale.

**EC-02:** User signs in on multiple devices or browsers — each gets an independent session; all are valid concurrently.

**EC-03:** User signs out in one tab while another tab is open — the other tab's next request will get a 401 and redirect to sign-in.

**EC-04:** Session cookie is present but the server-side session has expired or been deleted — treated as unsigned in; redirected to sign-in.

**EC-05:** User enters email with different casing or spaces — normalized before lookup, same as registration normalization (F-1.1).

**EC-06:** User closes the browser without signing out — session persists until expiry; no forced re-auth on next visit within the session window.

**EC-07:** User navigates directly to a protected page without a session — redirected to sign-in; after sign-in, redirected back to the originally requested page (preserve original URL).

**EC-08:** Sign-out called when no session exists — idempotent; returns success and clears any stale cookie.

**EC-09:** User changes their password (future feature) — all existing sessions are invalidated; user must sign in again with new password on all devices.

**EC-10:** Expired sessions accumulate in storage — cleaned up automatically by TTL eviction (Redis) or a scheduled cleanup process (PostgreSQL), preventing unbounded growth.

## 9. Dependencies & Risks

| Type | Item |
|------|------|
| **Upstream** | F-1.1 (registration) — must exist; sign-in validates against accounts created by registration. |
| **Downstream** | F-2.1 (create draft event) and all subsequent organizer features depend on an active session for authorization. |
| **Risks** | Session storage choice (PostgreSQL vs Redis) may affect sign-out reliability — mitigate by ensuring server-side session deletion is authoritative. No brute-force protection at the application layer for MVP — mitigate with transport-level rate limiting if available. |

## 10. Assumptions

- Session duration follows a fixed configuration value, not user-configurable.
- Multiple concurrent sessions per user are allowed for MVP (no "log out everywhere" feature).
- No "remember me" / persistent-session option for MVP — session lifetime is the same for all sign-ins.
- The organizer home area (post-login destination) is the same route used after registration until a dedicated dashboard exists.
- Sign-in does not emit a domain event for MVP (no `EVT-UserSignedIn`); this may be added later for audit logging.
- Email normalization at sign-in matches the normalization applied at registration (F-1.1).

## 11. Out of Scope

- F-1.1 registration (prerequisite, already spec'd).
- F-1.3 profile management.
- F-1.4 optional attendee accounts.
- Forgot-password / reset-password flows.
- OAuth / social login (Google, Facebook, etc.).
- Email verification before sign-in is allowed.
- Multi-device session management UI (view active sessions, revoke individual sessions).
- "Remember me" / persistent sessions beyond configured timeout.
- Account lockout after repeated failed attempts.
- CAPTCHA or bot mitigation on the sign-in form.
- Audit logging of sign-in/sign-out events (deferred).
- Two-factor authentication (2FA).
- Password change flow (but invalidation of existing sessions on password change is specified in §3).

## 12. Resolved Decisions

| # | Question | Decision | Date |
|---|----------|----------|------|
| 1 | What is the default session duration? Is it a rolling window or absolute? | **Absolute** — fixed timeout from sign-in, not reset on activity. | 2026-06-16 |
| 2 | Should the post-login redirect preserve the originally requested URL? | **Yes** — preserve the original URL and redirect there after sign-in. | 2026-06-16 |
