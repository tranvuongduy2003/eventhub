---
doc_schema: eventhub-doc-v1
doc_kind: implementation_spec
doc_id: spec-20260713223209-guest-order-ticket-access-hardening
title: Guest Order and Ticket Access Hardening
status: proposed
last_updated: 2026-07-13
owner: builder
language: en
applies_to:
  - F-5.6
  - F-6.1
  - F-7.1
  - F-7.3
  - F-7.4
  - F-7.5
  - F-10.1
  - F-10.3
source_documents:
  - docs/product.md
  - docs/features.md
  - docs/technical.md
---

# Guest Order and Ticket Access Hardening

## Problem

EventHub currently treats sequential database order and ticket identifiers as anonymous access
credentials. A caller can enumerate `/api/orders/{orderId}`, `/api/orders/{orderId}/tickets`, and
the matching transfer, return, resend, and payment-start routes. Those paths expose order details,
ticket QR/admission codes, and holder contact data, and they permit ticket mutations without proving
that the caller is the ticket holder.

The ticket display also sends each full ticket code to a third-party QR-image service. A ticket code
is an admission credential and must not be disclosed to an unrelated service merely to render the
buyer-facing QR image.

This conflicts with F-5.6, F-7.3, F-7.4, F-7.5, F-10.1, and F-10.3; `technical.md` section 13;
and the `INV-40` through `INV-44` trust boundary. The existing ticket-delivery implementation spec
calls opaque access-token hardening a future concern; that is now source-spec drift and must not
override the active technical specification.

## Solution

Replace public numeric identifiers as authority with durable, cryptographically random, opaque
access capabilities. The solution distinguishes a buyer's order capability from a holder's ticket
capability so that a group buyer can access their order while a transferred ticket receives a fresh,
independently revocable holder capability.

The new public routes accept only opaque capabilities. Numeric ids may remain internal identifiers
and may appear in authorized organizer/admin responses, but must not be accepted as public proof of
ownership. Ticket-code QR rendering becomes entirely first-party/client-side; full ticket codes must
not be sent to any external image-generation or analytics endpoint.

## Source Alignment

| Source | Relevant requirement | This slice's response |
|---|---|---|
| `product.md` QG-3, QG-6, DEC-2 | Tickets must be trustworthy, transfers fair, and personal data handled responsibly. | Scope all guest access and mutation authority to opaque capabilities; preserve face-value-only transfer behavior. |
| `features.md` F-5.2, F-5.6 | Guest checkout and accountless order status remain available. | No sign-in is introduced; public access changes from numeric references to opaque capabilities. |
| `features.md` F-7.1, F-7.3, F-7.4, F-7.5 | Tickets are uniquely issued, accessible without an account, clear on mobile, and recoverable. | Issue order/ticket capabilities with tickets, deliver capability links, expose a neutral recovery path, and keep QR rendering local. |
| `features.md` F-10.1, F-10.3 | Only a legitimate holder can transfer or return a ticket. | Transfer and return require the ticket-holder capability or a separately authenticated equivalent; transfer rotates the capability. |
| `technical.md` ARCH-8, section 13 | Boundary retries are idempotent; ticket access links and codes are unguessable and minimally scoped. | Generate durable capability values with cryptographic randomness, persist them safely, and never use numeric ids as anonymous authority. |

## In Scope

- A durable opaque order access capability issued with every newly placed order.
- A durable opaque ticket-holder capability issued with every ticket and rotated when a ticket is
  transferred.
- Public order-status, ticket-display, resend/recovery, payment-start/return, transfer, and return
  routes migrated from numeric-id authority to scoped capabilities.
- Contract, frontend, email-link, and browser-journey changes necessary to make the secure access
  paths usable on mobile without sign-in.
- A migration/backfill and legacy-link behavior that avoids exposing data through numeric URLs.
- First-party QR rendering that never transmits a full ticket code off-origin.
- API, persistence, application, and end-to-end evidence for valid, invalid, expired/revoked, and
  transferred capabilities.

## Out of Scope

- Selecting a real payment provider, provisioning provider credentials, or verifying provider
  webhooks. Those belong to the subsequent paid-checkout repair slice.
- Repairing multi-ticket reservation confirmation, duplicate-line purchase-limit enforcement, or
  order/return refund accounting.
- Adding a production email provider or changing the configured delivery provider. This slice may
  invoke the existing delivery port and must make its intent/idempotency observable, but real email
  delivery is a separate source-backed gap.
- A broad attendee-account or identity-model redesign.
- A paid resale marketplace, transfer price, or transfer fee.
- Changing organizer/staff authorization semantics for protected operations.

## Personas and Permissions

| Persona | Required behavior |
|---|---|
| Guest buyer | Can place an order, receive an opaque order link, inspect only that order, begin/retry payment for only that order, and view the tickets they still hold without creating an account. |
| Ticket holder / recipient | Can open only their ticket through its opaque holder link and can transfer or return only that ticket when its domain rules permit it. |
| Signed-in attendee | Continues to use authenticated wallet routes. The backend must not rely on a frontend route guard; privileged wallet mutations use server-side identity checks or the holder capability. |
| Organizer or Staff | Continue to use existing event-scoped authorization. They do not obtain guest capabilities merely by knowing numeric ids. |
| Attacker or unrelated visitor | Cannot infer whether a numeric order/ticket id exists, retrieve attendee data, obtain a QR code, start payment, resend, transfer, or return a ticket. |

## Dependencies

- Existing `Order`, `Ticket`, and `Contact` aggregates and their PostgreSQL persistence mappings.
- A cryptographically secure capability generator owned by an Application port and implemented in
  Infrastructure with no new production dependency.
- A new append-only EF Core migration for durable capability fields/indexes and an explicit
  backfill/recovery strategy for existing orders and tickets.
- The existing `IEmailSender` port for capability-link delivery/recovery intent.
- An approved first-party QR encoder. The repository has no such encoder today; adding one requires
  explicit dependency approval and a permissive-license check before implementation.

## Acceptance Criteria

1. **Opaque guest order access**

   GIVEN a guest places an order, WHEN the order is accepted, THEN the response contains an
   unguessable opaque order access capability or URL, and no public response requires the numeric
   order id as proof of authority.

2. **Scoped order status**

   GIVEN a valid order access capability, WHEN a guest opens its order status, THEN they see only
   that order's permitted status, final price, and line-item data; WHEN the capability is missing,
   malformed, revoked, or belongs to another order, THEN the API returns a non-enumerating public
   failure without data, ticket codes, holder details, or an existence distinction.

3. **Scoped payment continuation**

   GIVEN a valid order access capability for a pending paid order, WHEN the buyer starts or retries
   payment, THEN only that order's payment attempt can be initiated and provider return URLs retain
   the same opaque order access capability. A numeric order id alone cannot start payment.

4. **Accountless ticket access**

   GIVEN a valid ticket-holder capability, WHEN the holder opens it without signing in, THEN they
   can view that ticket's event details and QR code on a phone. An order-level view may show only
   tickets the order buyer is still entitled to access and must direct each transferable ticket to
   its individually scoped link.

5. **No external ticket-code disclosure**

   GIVEN a ticket is displayed, WHEN its QR image is rendered, THEN no network request to a
   third-party origin contains the ticket code, an order capability, holder data, or derived bearer
   credential. The rendered QR remains readable at a phone-sized viewport.

6. **Ticket-holder mutation authority**

   GIVEN a ticket holder has a valid capability for a valid ticket, WHEN they request a transfer or
   eligible return, THEN the existing domain rules run for that ticket only. GIVEN no valid holder
   capability, a numeric order id, a numeric ticket id, or a capability for a different ticket,
   THEN the mutation is refused without revealing the target's state or holder data.

7. **Transfer rotation and least privilege**

   GIVEN a valid ticket is transferred, WHEN the transfer commits, THEN the old ticket capability
   no longer grants ticket access or mutation; a fresh capability is issued only for the replacement
   ticket and is delivered through the existing notification boundary. The original buyer's order
   capability cannot reveal the replacement holder's contact data or QR code.

8. **Resend and recovery**

   GIVEN a buyer requests recovery/resend using the supported contact proof, WHEN the request is
   valid, THEN EventHub sends a current opaque capability link without issuing duplicate tickets.
   GIVEN unknown or non-matching input, THEN the public response remains neutral and does not reveal
   whether an order, email, ticket, or capability exists.

9. **Legacy numeric links**

   GIVEN a caller visits an old numeric-only public order or ticket path after the migration policy
   takes effect, WHEN no valid opaque capability accompanies it, THEN no order/ticket data or
   mutation is available. The exact status/redirect/recovery behavior must follow the explicitly
   approved migration policy; it must never silently preserve numeric bearer access.

10. **Persistence, uniqueness, and retry behavior**

    GIVEN capability generation, backfill, transfer, or recovery is retried, WHEN it executes, THEN
    persisted capabilities remain unique, active capabilities resolve to one authorized subject,
    rotated/revoked capabilities cannot regain authority, and retrying the operation does not create
    duplicate tickets or duplicate active capability records.

11. **Guest UX and accessibility**

    GIVEN a guest follows a valid link on a phone, WHEN they view an order or ticket, THEN the
    page explains its state without exposing implementation/security details, shows a clear recovery
    action for invalid/expired access, supports keyboard navigation, and does not require account
    creation for F-5.2/F-7.3 behavior.

12. **Automated evidence**

    GIVEN the hardened routes are implemented, WHEN API integration and browser journeys run, THEN
    they prove valid guest access, anonymous numeric-id denial, cross-order/cross-ticket denial,
    transfer rotation, neutral recovery, first-party QR rendering, and an unauthenticated
    guest-checkout-to-ticket journey.

## Business Rules and Invariants

- A public capability is a bearer credential. It MUST be generated from at least 128 bits of
  cryptographically secure randomness, be URL-safe, be unique while active, and never be derived
  from an order id, ticket id, email, timestamp, or a predictable sequence.
- Numeric order and ticket ids are internal references, never anonymous authorization credentials.
- Order capabilities authorize only one buyer/order scope. Ticket capabilities authorize only one
  current ticket-holder scope. An implementation must not widen a ticket capability into authority
  over unrelated tickets or orders.
- A transferred, void, returned, cancelled, or otherwise non-valid ticket capability cannot be used
  to admit, transfer, return, or disclose a replacement ticket.
- Capability checks belong in Application-level authorization/orchestration and are enforced by the
  backend on every public read or mutation; frontend routes and hidden numeric ids are not security
  controls.
- Capabilities, ticket codes, contact details, and provider references MUST NOT appear in logs,
  ProblemDetails, telemetry, analytics URLs, or third-party resource URLs.
- Existing `INV-20` through `INV-25` and `INV-40` through `INV-44` remain in force. This slice
  does not weaken ticket-code uniqueness, exactly-once admission, fair transfer, or return rules.

## Domain, Application, and Persistence Design Notes

- Introduce typed Domain value objects for order and ticket access capabilities, with validation
  that rejects malformed or empty input. Capability creation may be coordinated by Application
  through an Infrastructure randomness port; Domain must not depend on a cryptographic library or
  HTTP transport.
- Persist the minimum durable capability state needed for lookup, rotation, revocation, and
  recovery. If hashes rather than raw capability values are stored, the design must preserve a safe
  way to send/recover links without reintroducing a predictable identifier. Do not store a
  capability only in browser storage or a volatile cache.
- Add an append-only migration with a reviewed rollback/data plan. Existing migrations and snapshots
  are immutable. The migration plan must account for existing records before public numeric routes
  are disabled.
- Repository lookup methods must resolve by scoped capability without leaking whether an unrelated
  numeric id exists. PostgreSQL remains authoritative; Redis may not decide access.
- Create separate Application commands/queries or adapt existing ones so each handler receives an
  access capability rather than a numeric id for public behavior. Expected capability failures use
  stable, non-enumerating `Result` errors.
- Capability rotation caused by a transfer must be transactional with ticket state changes. Delivery
  of a new link occurs only after the durable ticket/capability state is committed and is idempotent
  at the logical recipient/message boundary.

## API and Contract Expectations

- Public routes must use opaque capability path/query values or a dedicated request body, never
  `/api/orders/{numericOrderId}` as bearer authority. Keep route naming clear enough that generated
  clients distinguish internal ids from public capabilities.
- The placement response and public status/ticket responses must expose the appropriate public URL
  or capability only to the caller that already proved/received it. They must not expose capability
  hashes, numeric identifiers as access keys, or another holder's access capability.
- Transfer and return operations must accept a ticket capability or a server-authenticated equivalent
  and must not treat `{orderId}/{ticketId}` alone as authority.
- Recovery/resend must use an explicitly documented, neutral response (normally `202`) and avoid
  `404`/validation distinctions that enable order or email enumeration.
- Update `contracts/openapi/api.v1.yaml`, Contracts DTOs, endpoint metadata, frontend API callers,
  and generated types together. Do not hand-edit `web/src/generated/**`.

## Frontend Expectations

- Replace numeric public routes with opaque capability routes while retaining readable, friendly
  headings and error states. Do not render full sensitive capabilities in visible UI text.
- Update checkout success/cancel URLs and links on order-status, ticket, recovery, transfer, and
  return surfaces to carry only the intended capability.
- Preserve guest checkout: after placement, the buyer remains able to navigate to their order and
  tickets without a login prompt.
- Implement QR rendering entirely within the application origin. The chosen implementation must be
  accessible, render a scan-friendly image/canvas/SVG, have useful alternative text, and avoid
  network disclosure of the code. Do not add a production dependency without explicit approval and
  a license check.
- Use generated OpenAPI types for changed public shapes and preserve loading, pending, neutral
  recovery, error, success, keyboard, and narrow-screen states.

## Integration and Messaging Expectations

- The existing email port remains the boundary for sending order/ticket capability links. Delivery
  is not the access source of truth; PostgreSQL capability state is authoritative.
- New/rotated links must be generated from committed durable state. A failure before commit must not
  send a link to a nonexistent or invalid ticket.
- Recovery and resend attempts must be deduplicated enough to avoid duplicate logical sends on
  retries; rate limiting/abuse protection may be added only if it follows an existing platform
  pattern or receives separate scope approval.

## Security, Privacy, and Abuse Considerations

- Treat every capability and ticket QR code as secret bearer data. Redact or avoid them in logs,
  errors, browser console output, telemetry, test fixtures, screenshots, and external requests.
- Generic invalid-access behavior should be indistinguishable for unknown, revoked, malformed, and
  cross-scope capabilities unless a legitimate holder has already been authenticated by a valid
  capability.
- Public mutation routes must verify the capability before looking up or returning detailed ticket
  state where practical, and must prevent confused-deputy behavior between an order capability and
  a ticket-holder capability.
- Existing authenticated wallet behavior should use the authenticated identity for authorization;
  it must not continue to rely on anonymous numeric routes merely because the UI is protected.

## Edge Cases and Failure Modes

- A buyer opens a valid order link on another device or browser: access still works without a
  session and without exposing any other order.
- A copied ticket link works only for the intended ticket while active; after transfer it becomes
  invalid and the recipient's new link works.
- An order has multiple tickets: individual ticket links remain independently shareable, while the
  order view does not disclose tickets no longer held by its buyer.
- A ticket is checked in, voided, refunded, or returned: its capability can no longer enable a
  forbidden mutation, and its display uses the correct non-admission state.
- A guest returns from provider checkout with an opaque order capability: the status page remains
  accessible even though payment confirmation may arrive asynchronously.
- A legacy numeric link is visited during or after migration: no sensitive data is served; recovery
  behavior follows the approved policy.
- QR rendering fails locally: the UI provides a safe readable fallback and retry/error state without
  making an external request containing the code.

## Testing and Verification Strategy

### Backend and contract

- Domain tests for capability value-object validation and ticket capability rotation/revocation.
- API integration tests for generation, persistence, uniqueness, opaque route access, invalid and
  cross-scope denial, public numeric route denial, public payment start, transfer, return, recovery
  neutrality, and post-transfer access isolation.
- Migration/integration tests proving existing records receive the approved handling without editing
  an existing migration.
- Tests that inspect captured email-port messages for a capability link only after the corresponding
  order/ticket state is committed, using fictional data and never printing raw capability values.
- Run `yarn --cwd web api:export`, `yarn --cwd web api:verify`, focused API tests, then the full
  .NET suite.

### Web and browser

- Unit/component evidence for local QR rendering that does not request `api.qrserver.com` or any
  other external origin with a ticket code.
- Playwright journey: unauthenticated attendee completes a free guest checkout, follows the opaque
  order/ticket link in a clean browser context, sees their ticket QR, and never signs in.
- Playwright negative journey: replacing an opaque capability with a numeric id or a different
  capability exposes no order/ticket content and cannot transfer or return a ticket.
- Playwright transfer journey: original link loses access to the transferred ticket; the recipient's
  delivered capability accesses the replacement ticket.
- Run the focused Playwright files, then `yarn --cwd e2e test` when shared routes/fixtures change.

## Risks and Follow-up Decisions

| Decision or risk | Required resolution |
|---|---|
| Existing numeric-only links | **Human approval required before implementation.** Choose an explicit migration policy that may invalidate public numeric links and use neutral recovery/resend; numeric bearer access must not remain. |
| QR encoder | **Human approval required before implementation** if a new production dependency is needed. Record its permissive license. Do not retain the third-party QR-image service as a workaround. |
| Real email delivery | The local sender does not deliver external mail. A production email-provider decision/configuration is needed before claiming real-world recovery/link delivery is complete. |
| Capability storage approach | The implementation plan must select and justify raw-versus-hashed storage, token rotation, migration/backfill, and recovery mechanics without weakening link durability or secrecy. |
| Paid checkout | Provider callback signature verification and deterministic local paid completion are intentionally deferred to the separate payment repair spec, but its return URL must consume this slice's opaque order capability. |

## Implementation Planning Notes

- This is a security-sensitive, cross-layer change: Domain, Application, Infrastructure, Api,
  Contracts, OpenAPI, web, tests, and E2E are all required areas.
- Run `security-reviewer` and `code-reviewer` after every implemented slice; run
  `acceptance-verifier` against this spec before marking it complete.
- Create a feature branch and initialize the coding loop only after the human approval boundary is
  satisfied. The plan must list migration rollback/data safety, exact public route replacements,
  first-party QR implementation, and browser evidence as non-optional slices.
