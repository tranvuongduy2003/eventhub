---
artifact_type: spec
artifact_version: 1
id: spec-20260616120000-manage-organizer-profile
title: Manage organizer profile
slug: manage-organizer-profile
filename_template: 20260616120000-manage-organizer-profile.md
created_at: "2026-06-16T12:00:00Z"
updated_at: "2026-06-16T12:30:00Z"
status: draft
owner: product
tags: [spec, eventhub, ep-1-organizer-accounts]
feature_refs: [F-1.3]
ddd_refs: [BC-1, AGG-User, VO-DisplayName, VO-EmailAddress, VO-AvatarImageRef, INV-1]
prd_refs: [DEC-1, QG-1, QG-6]
tech_refs: [Tech §5, Tech §6, Tech §7]
db_refs: [Tech §6]
github_issue: 8
search_index:
  keywords: [profile, display name, email, avatar, image upload, organizer, settings, MinIO]
  bounded_contexts: [Identity & Access]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: #8 (https://github.com/tranvuongduy2003/eventhub/issues/8)

# Feature: Manage organizer profile

> Features: F-1.3  |  Status: DRAFT  |  Date: 2026-06-16
> PRD: DEC-1, QG-1, QG-6  |  DDD: BC-1, AGG-User  |  Tech: §5, §6, §7

## 1. Problem & Solution

**Problem:** After registering (F-1.1), an organizer has no way to update their display name, change their contact email, or personalize their account with an avatar. Their profile is frozen at registration time.

**Solution:** A profile management screen where the organizer can edit their display name, update their contact email, and upload an avatar image. Changes are reflected immediately across the platform (event pages, check-in, organizer area).

**Personas:** PER-O1 (individual organizer), PER-O2 (small group/club organizer)

**Scope:**
- **In:** F-1.3 — edit display name, edit contact email, upload/remove avatar
- **Out:** F-1.4 (attendee account — Later), password change (separate concern), profile visibility settings

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in, WHEN I update my display name with a valid value (1–64 characters, trimmed), THEN the new display name is saved and shown on my profile and subsequent screens.

**AC-02:** GIVEN I am signed in, WHEN I update my display name to an empty string or more than 64 characters, THEN I am rejected with a clear message and my profile remains unchanged.

**AC-03:** GIVEN I am signed in, WHEN I update my contact email to a new valid, unique email, THEN the email is saved and shown; a confirmation or re-verification step is not required in the MVP (the email is accepted as-is).

**AC-04:** GIVEN I am signed in, WHEN I update my contact email to an email already taken by another account, THEN I am rejected with an "email taken" message and my profile remains unchanged.

**AC-05:** GIVEN I am signed in, WHEN I update my contact email to a malformed email address, THEN I am rejected with a clear message and my profile remains unchanged.

**AC-06:** GIVEN I am signed in, WHEN I upload a supported image file (JPEG, PNG, or WebP) within the size limit (5 MB), THEN the image is stored, a reference is saved on my profile, and the avatar is displayed on my profile and event pages I own.

**AC-07:** GIVEN I am signed in, WHEN I upload an unsupported file type or a file exceeding the size limit, THEN I am rejected with a clear message indicating what was wrong and no avatar is changed.

**AC-08:** GIVEN I am signed in and have an avatar, WHEN I remove my avatar, THEN the reference is cleared and the default/placeholder avatar is shown instead.

**AC-09:** GIVEN I am signed in, WHEN I view my profile, THEN I see my current display name, contact email, and avatar (or placeholder if none).

**AC-10:** GIVEN I update any profile field, WHEN the save succeeds, THEN the updated values are immediately visible on my profile page and in the `GET /api/auth/me` response.

## 3. Domain & Business Rules

**Bounded context:** BC-1 (Identity & Access)

**Aggregate:** AGG-User

**Value objects involved:**
- `VO-DisplayName` — 1–64 characters, trimmed, Unicode allowed, non-unique (already exists)
- `VO-EmailAddress` — well-formed, normalized, unique across accounts (already exists)
- `VO-AvatarImageRef` — object key/URL into object storage; never the bytes; nullable (new)

**Invariants:**
- `INV-1` — Email must be unique across all users. An update to an email already held by another user is rejected.
- Display name follows the same rules as at registration (1–64 chars, trimmed).
- Avatar is optional; at most one avatar reference per user.

**Behavior:** `UpdateProfile(displayName?, email?, avatarImageRef?)` on the User aggregate. The aggregate validates each supplied field against its value object rules. Only supplied fields change; omitted fields remain unchanged (partial update semantics — PATCH, not PUT).

**Domain event:** `EVT-UserProfileUpdated` — raised on any successful profile change. Domain-scope (in-process); no integration event needed for MVP since no other bounded context consumes profile changes synchronously.

**Storage principle (product-requirements.md QG-6, technical-design.md §5):** Avatar image bytes are stored in MinIO (object storage). The User aggregate persists only the object key/URL reference (`VO-AvatarImageRef`), never the binary content.

## 4. UI Behavior

**Profile page / settings screen:**
- Accessible from the organizer area (requires sign-in).
- Displays current display name, contact email, and avatar (or placeholder).
- Each field is editable; changes are submitted together or individually.
- Avatar section shows current avatar with an upload button and (if an avatar exists) a remove button.
- Success feedback: inline confirmation or updated values reflected immediately.
- Error feedback: inline field-level messages for validation failures; toast/banner for server errors.

**API contract:**
- `PATCH /api/users/me` — partial update. Only include fields that should change. Omitted fields remain unchanged. Request body: `{ displayName?, email? }`.
- `POST /api/users/me/avatar` — multipart file upload for the avatar. Replaces any existing avatar.
- `DELETE /api/users/me/avatar` — removes the current avatar.
- `GET /api/auth/me` — already exists; updated to include `avatarUrl` in the response.

**Avatar upload flow:**
1. Organizer selects a file from their device.
2. Client validates file type (JPEG, PNG, WebP) and size (≤ 5 MB) before upload; rejects immediately with a message if invalid.
3. On valid selection, the file is uploaded to the server; the server stores it in MinIO and returns the reference.
4. The profile update command includes the new avatar reference.

**Mobile consideration (QG-4):** The profile page is usable on a phone — fields stack vertically, avatar upload works from the device camera roll or file picker.

## 5. Data & Storage Impact

**PostgreSQL (app schema):**
- `users` table: add `avatar_image_ref` column (nullable text, stores the MinIO object key). No schema change to existing columns.
- Migration: append-only; new migration adds the column.

**MinIO (object storage):**
- New bucket or prefix for avatars (e.g., `avatars/{userId}/`).
- Object key pattern: `avatars/{userId}/{unique-filename}.{ext}`.
- Old avatar is deleted from MinIO when replaced or removed (cleanup; no orphaned objects).

**Redis:** Session cache (`/api/auth/me`) is invalidated/updated after a successful profile change so the next read reflects the new values. Handled by the existing `PostCommitSessionCacheBehavior` pipeline.

**No changes to RabbitMQ or other stores.**

## 6. Real-Time & Consistency

**N/A for this feature.** Profile changes do not need real-time push to other clients. The organizer sees their own changes immediately via the command response; other views (e.g., event pages they own) reflect the change on next load.

Consistency is strong within the User aggregate (single transaction). The session cache is updated post-commit by the existing pipeline behavior.

## 7. Security & Privacy

**Session required:** All profile operations require an active session (F-1.2). Unauthenticated requests are rejected with 401.

**Ownership:** An organizer can only update their own profile. The command resolves the current user from the session; there is no "update another user's profile" path.

**Email uniqueness (INV-1):** Enforced at the domain level and backed by a unique database constraint. Prevents account takeover via email collision.

**Avatar uploads (QG-6):**
- File type is validated on both client and server (content-type check; optionally magic-byte verification).
- File size is capped at 5 MB to prevent abuse.
- Stored in MinIO with appropriate access control; public read for display, write only through the application.
- No personal data is embedded in the image metadata by the application (metadata stripping is a future consideration, not MVP).

**No payment boundary involvement (DEC-1).**

## 8. Edge Cases

**EC-01:** Organizer updates email to the same email they already have. → Treated as a no-op; no error, no uniqueness violation (the existing record is the same user).

**EC-02:** Organizer uploads a very large image (close to 5 MB). → Server accepts if within limit; MinIO handles it. Client-side resize/compression is a future enhancement, not MVP.

**EC-03:** Organizer removes avatar they never had. → The remove action is a no-op or disabled in the UI; no error.

**EC-04:** Two organizers try to update to the same email simultaneously. → One succeeds; the other gets "email taken" on the uniqueness constraint. Optimistic concurrency on the User aggregate handles the write race.

**EC-05:** MinIO is unavailable during avatar upload. → Upload fails with a server error; the profile update without the avatar can still succeed (avatar is optional). The error message tells the user to try again later.

**EC-06:** Organizer uploads a file with a valid extension but invalid content (e.g., a `.jpg` that is actually a text file). → Server-side content-type/magic-byte check rejects it with a clear message.

**EC-07:** Display name contains leading/trailing whitespace. → Value object trims automatically; the stored value is clean.

## 9. Dependencies & Risks

**Dependencies:**
- F-1.1 (register organizer account) — must be complete. ✅ Done.
- F-1.2 (sign in) — must be complete for session-based auth. ✅ Done.
- MinIO adapter — currently a `NoOpObjectStorage` stub. Will be replaced with a real MinIO adapter as part of this feature's build scope. ✅ Resolved — in scope.

**Risks:**
- **Email re-verification.** The spec accepts email changes without re-verification (MVP simplicity). This is acceptable for a pet project (ASM-1) but would need revisiting for production.

## 10. Assumptions

1. Email changes do not require re-verification (MVP; acceptable per ASM-1 — solo builder).
2. Avatar file size limit is 5 MB (sufficient for profile photos; configurable later).
3. Supported image types: JPEG, PNG, WebP (covers modern browsers and devices).
4. Old avatar objects are cleaned up from MinIO when replaced or removed (no retention policy needed).
5. The `PostCommitSessionCacheBehavior` pipeline handles session cache invalidation after profile updates — no custom cache logic needed.
6. Display name uniqueness is not required (already decided in F-1.1 — `DisplayName` is non-unique).
7. The MinIO adapter is implemented as part of this feature (resolved OQ-1). It replaces the `NoOpObjectStorage` stub and will also serve future features that need object storage (e.g., F-2.2 cover image).

## 11. Out of Scope

- Password change (separate feature; not part of F-1.3).
- Attendee profile or account (F-1.4 — Later phase).
- Profile visibility settings or privacy controls.
- Image cropping, resizing, or client-side compression.
- Email re-verification flow.
- Profile changes propagating via integration events to other bounded contexts (not needed until attendee accounts or notifications consume profile data).

## 12. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should the MinIO adapter be implemented as part of this feature, or should avatar upload be deferred until MinIO is wired for another feature (e.g., F-2.2 cover image)? | ✅ Resolved — implement MinIO in this feature |
| 2 | Should the API support partial updates (PATCH — only send changed fields) or full replacement (PUT — send all fields)? | ✅ Resolved — partial updates (PATCH) |
