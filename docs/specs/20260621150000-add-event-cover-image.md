---
artifact_type: spec
artifact_version: 1
id: spec-20260621150000-add-event-cover-image
title: Add an event cover image
slug: add-event-cover-image
filename_template: 20260621150000-add-event-cover-image.md
created_at: 2026-06-21T15:00:00Z
updated_at: 2026-06-21T15:00:00Z
status: draft
owner: product
tags: [spec, eventhub, event-management]
feature_refs: [F-2.2]
ddd_refs: [BC-2, AGG-Event, VO-CoverImageRef]
prd_refs: [DEC-3, QG-1, QG-4]
tech_refs: [Tech §5]
db_refs: [Tech §6]
github_issue: 23
plan_ready: true
search_index:
  keywords: [cover image, event, upload, image, organizer, MinIO, object storage, file upload]
  bounded_contexts: [Event Management]
  user_personas: [PER-O1, PER-O2]
---

> GitHub: [#23](https://github.com/tranvuongduy2003/rebuilt-eventhub/issues/23)

# Feature: Add an event cover image

> Features: F-2.2  |  Status: DRAFT  |  Date: 2026-06-21
> PRD: DEC-3 (MVP scope), QG-1 (simplicity), QG-4 (mobile-friendly)
> DDD: BC-2 Event Management, AGG-Event, VO-CoverImageRef
> Tech: §5 (MinIO object storage)

## 1. Problem & Solution

**Problem:** An event without a visual looks incomplete and untrustworthy. Organizers need a way to add a compelling cover image that appears on the public event page and in link previews, making the event more attractive to potential attendees.

**Solution:** Allow the event owner to upload a cover image for their event. The image is stored in object storage (MinIO), and the event record keeps only a reference to it. The cover image is displayed on the public event page (EP-4) and contributes to rich link previews (F-4.3).

**Personas:** PER-O1 (individual organizer), PER-O2 (small group/club organizer).

**Scope:**
- **In scope (F-2.2):** Upload a cover image for a draft or published event; replace an existing cover image; reject invalid files.
- **Out of scope:** Cropping/resizing in the browser (deferred); multiple images/gallery (not in roadmap); image CDN optimization (not in scope for MVP).

## 2. Acceptance Criteria

**AC-01:** GIVEN I am signed in and hold the Owner role for an event in Draft or Published status (F-1.5), WHEN I upload a supported image file (JPEG, PNG, or WebP) within the size limit (max 5 MB) and resolution limit (max 1920×1080), THEN the image is stored and set as the event's cover image, and I see it displayed on my event.

**AC-02:** GIVEN I am signed in but do NOT hold the Owner role for an event, WHEN I attempt to upload a cover image for that event, THEN I am refused with an "insufficient permissions" message and no file is stored.

**AC-03:** GIVEN I am signed in and hold the Owner role, WHEN I upload a file that is not a supported image format (e.g., PDF, GIF, SVG, EXE), THEN the upload is rejected with a clear message stating which formats are accepted.

**AC-04:** GIVEN I am signed in and hold the Owner role, WHEN I upload an image that exceeds the size limit (5 MB), THEN the upload is rejected with a clear message stating the maximum allowed size.

**AC-05:** GIVEN an event already has a cover image, WHEN I upload a new cover image, THEN the new image replaces the old one as the event cover, the old image reference is removed, and the old file is deleted from storage immediately.

**AC-06:** GIVEN a published event has a cover image, WHEN anyone visits the public event page (F-4.1), THEN the cover image is displayed prominently at the top of the page.

**AC-07:** GIVEN a draft event has a cover image, WHEN the owner views the event in the organizer area, THEN the cover image is shown as a preview of how it will appear on the public page.

**AC-08:** GIVEN I upload a cover image, WHEN the upload succeeds, THEN the response confirms the image was stored and returns the reference/URL so the UI can display it immediately.

**AC-09:** GIVEN I am signed in and hold the Owner role, WHEN I upload an image that exceeds the maximum resolution (1920×1080), THEN the upload is rejected with a clear message stating the maximum allowed dimensions.

**AC-10:** GIVEN I am signed in and hold the Owner role for a Cancelled event, WHEN I attempt to upload a cover image, THEN the upload is rejected with a message that cover images can only be set on Draft or Published events.

## 3. Domain & Business Rules

**Domain model alignment (ddd.md):**
- The `Event` aggregate (AGG-Event) in BC-2 owns the `VO-CoverImageRef` value object — an object key/URL into object storage, never the bytes themselves.
- The `SetCoverImage` behavior on `Event` is the domain-level operation that updates the cover image reference.

**Invariants:**
- Only the event owner (Owner role per F-1.5) can set or change the cover image. This is an authorization check in the Application handler, not a domain invariant.
- The domain does not validate file format, size, or resolution — that is an application/infrastructure concern (validating before storing).
- Cover image upload is restricted to Draft or Published events. Cancelled events cannot have their cover image changed. This is enforced in the Application handler as a status guard.

**Ownership model:**
- F-1.5 defines that each event has exactly one Owner. The cover image upload checks the caller holds the Owner role for the target event.
- Staff users (F-1.6) cannot upload cover images.

## 4. UI Behavior

**Organizer — upload flow:**
1. On the event edit page (both Draft and Published states), the organizer sees a cover image section.
2. If no cover image exists, a prominent upload area is shown (drag-and-drop or click to browse).
3. If a cover image already exists, the current image is shown with an option to replace it.
4. On selecting a file, client-side validation checks format and size before uploading. If invalid, an inline error message appears immediately.
5. On upload, a progress indicator is shown. On success, the new image is displayed. On failure (server rejection), the error message from the API is shown inline.

**Public event page (EP-4):**
- The cover image is displayed prominently at the top of the event page, above the title and details.
- On mobile (QG-4), the image scales to fill the viewport width with appropriate aspect ratio.
- If no cover image is set, a default placeholder is shown.

**Design notes (design-system.md):**
- Use Tailwind semantic tokens; no raw hex.
- Image should use `object-cover` for consistent aspect ratio across screen sizes.
- Loading skeleton while image fetches.

## 5. API Contract

**Endpoint:** `PUT /api/events/{eventId}/cover-image`

- **Auth:** Cookie session required. Caller must hold the Owner role for the event.
- **Request:** `multipart/form-data` with a single file field (`file`).
- **Constraints:**
  - Accepted formats: `image/jpeg`, `image/png`, `image/webp`
  - Maximum size: 5 MB
- **Success (200):** Returns the event's updated cover image reference (URL/key).
- **Error (401):** Not authenticated.
- **Error (403):** Insufficient permissions (not the Owner).
- **Error (404):** Event not found.
- **Error (422):** Invalid file (wrong format, exceeds size limit, or exceeds resolution limit). RFC 7807 with a stable `code` (e.g., `COVER_IMAGE_FORMAT_UNSUPPORTED`, `COVER_IMAGE_TOO_LARGE`, `COVER_IMAGE_RESOLUTION_EXCEEDED`, `EVENT_STATUS_NOT_ALLOWED`).

**OpenAPI:** Add the endpoint to `contracts/openapi/api.v1.yaml` after implementation.

## 6. Data & Storage Impact

**PostgreSQL (Tech §6):**
- The `Events` table already stores `CoverImageKey` (or equivalent column) — a string holding the MinIO object key. No schema change needed if the column exists from F-2.1; otherwise, a migration adds it.

**MinIO (Tech §5):**
- Uploaded images are stored in the configured bucket under a path like `events/{eventId}/cover/{filename}`.
- Only the object key is persisted in PostgreSQL; the bytes live in MinIO.
- Old cover images are not immediately deleted on replacement (cleanup can be a deferred background task).

**Redis:** No direct impact. The event cache (if any) is invalidated or rebuilt after the image reference changes.

## 7. Real-Time & Consistency

**N/A for this feature.** Cover image changes do not require real-time push (EP-11). The updated image is visible on the next page load or query.

If the event is cached in Redis, the cache entry should be invalidated when the cover image changes so the public page shows the new image promptly.

## 8. Security & Privacy

- **Authentication:** Only signed-in users can upload. Cookie session (F-1.2).
- **Authorization:** Only the Owner role for the event can upload (F-1.5). Checked in the Application handler.
- **File validation:** Server-side validation of MIME type and file size. Do not trust client-side validation alone.
- **No direct file serving from PostgreSQL:** Images are served from MinIO, keeping the database lightweight.
- **No executable content:** SVG is excluded because it can contain scripts. GIF is excluded for simplicity (animated images are out of scope).
- **Guest access:** Public event visitors can view the cover image (no auth required for viewing).

## 9. Edge Cases

**EC-01:** The organizer uploads a valid image, then immediately uploads another one before the first upload completes. → The second upload should either queue or fail gracefully; the final state should have exactly one cover image.

**EC-02:** The file extension says `.jpg` but the MIME type is `application/pdf`. → Server-side MIME detection rejects the file; do not rely on extension alone.

**EC-03:** The image is exactly 5 MB. → Accepted (the limit is inclusive).

**EC-04:** The image is 5 MB + 1 byte. → Rejected with a clear size-limit message.

**EC-05:** The organizer tries to upload without selecting a file (empty request). → Rejected with a clear message that a file is required.

**EC-06:** The event is in Cancelled status. → Cover image upload should still be allowed (the owner may want to update the page for context), or blocked — this is a product decision. For MVP, allow it (the event page exists regardless).

**EC-07:** MinIO is temporarily unavailable during upload. → Return a 500 with a generic error; the organizer can retry.

**EC-08:** The organizer uploads a very large image (e.g., 20 MB photo from a camera). → Rejected at the API layer before the file is fully read (use `MultipartBodyLengthLimit` or equivalent).

## 10. Dependencies & Risks

**Dependencies:**
- F-2.1 (Create a draft event) — the event must exist before a cover image can be uploaded.
- F-1.5 (Define roles and permissions) — the Owner role must be defined and assignable.
- MinIO provisioning via Aspire AppHost (Tech §5) — must be running and accessible.

**Risks:**
- **MinIO not running:** If the object storage container is down, uploads fail. Mitigation: health check in Aspire dashboard; clear error message to the user.
- **Large file uploads on slow connections:** Mitigation: client-side validation catches obviously oversized files early; server-side limit prevents abuse.
- **Image format detection ambiguity:** Some files may have mismatched extensions and content types. Mitigation: use content-type detection (magic bytes), not just the declared type.

## 11. Assumptions

- MinIO is provisioned and accessible in the local development environment (Aspire AppHost).
- The `Event` aggregate already has a field for the cover image reference (from F-2.1 domain model).
- No image processing (resize, crop, optimize) is needed for MVP — the original file is stored and served as-is.
- The 5 MB limit is sufficient for a single cover image on a small event page.
- **Maximum resolution:** 1920×1080 (HD). Images exceeding this dimension are rejected server-side.
- **Status guard:** Cover image upload is allowed only for Draft or Published events (not Cancelled).
- **Old image cleanup:** Replaced cover images are deleted from MinIO immediately on replacement.

## 12. Out of Scope

- Browser-side image cropping or resizing.
- Multiple images or a gallery per event.
- Image CDN or automatic optimization/serving at different resolutions.
- Animated images (GIF, APNG).
- SVG uploads (security risk — executable content).

## 13. Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Should cover image upload be allowed on Cancelled events, or only Draft/Published? | ✅ Resolved — Draft/Published only. Status guard enforced in handler. |
| 2 | What is the exact maximum resolution? Should the server reject extremely large dimensions? | ✅ Resolved — Max 1920×1080 (HD). Server rejects images exceeding this. |
| 3 | Should old cover images be deleted from MinIO immediately on replacement? | ✅ Resolved — Deleted immediately on replacement. |
