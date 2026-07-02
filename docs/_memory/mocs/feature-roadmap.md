---
title: Feature Roadmap MOC
type: moc
status: active
tags:
  - moc/features
  - product/features
---

# Feature Roadmap MOC

Authoritative source: [[_memory/source/feature-specification|feature specification]].

## Build spine

`EP-1 Accounts -> EP-2 Events -> EP-3 Ticketing -> EP-4 Discovery -> EP-5 Purchase -> EP-6 Payment -> EP-7 Delivery -> EP-8 Check-in`, then `EP-9 Audience`, `EP-10 Transfer`, and `EP-11 Realtime`.

## Current implementation specs

### EP-1 - Organizer Accounts & Identity

- `F-1.1` [[_memory/specs/20260616011034-register-organizer-account|Register an organizer account]]
- `F-1.2` [[_memory/specs/20260616020000-sign-in-and-sign-out|Sign in and sign out]]
- `F-1.3` [[_memory/specs/20260616120000-manage-organizer-profile|Manage organizer profile]]
- `F-1.4` [[_memory/specs/20260616150533-optional-attendee-account|Optional attendee account]]
- `F-1.5` [[_memory/specs/20260620100000-define-roles-and-permissions|Define roles and permissions]]
- `F-1.6` [[_memory/specs/20260620150000-assign-roles-to-users-per-event|Assign roles to users per event]]
- `F-1.7` [[_memory/specs/20260620200000-role-based-access-control-for-event-operations|Role-based access control for event operations]]
- `F-1.8` [[_memory/specs/20260620230000-invite-staff-to-event|Invite staff to an event]]
- `F-1.9` [[_memory/specs/20260620101500-permission-audit-log|Permission audit log]]

### EP-2 - Event Creation & Management

- `F-2.1` [[_memory/specs/20260619100000-create-draft-event|Create a draft event]]
- `F-2.2` [[_memory/specs/20260621150000-add-event-cover-image|Add an event cover image]]
- `F-2.3` [[_memory/specs/20260621180000-edit-event-details|Edit event details]]
- `F-2.4` [[_memory/specs/20260622000000-publish-event|Publish an event]]
- `F-2.5` [[_memory/specs/20260622143000-close-cancel-event|Close or cancel an event]]
- `F-2.6` [[_memory/specs/20260623120000-duplicate-event|Duplicate an event]]
- `F-2.7` [[_memory/specs/20260624142000-multiple-occurrences|Multiple occurrences / sessions]]

### EP-3 - Ticketing & Transparent Pricing

- `F-3.1` [[_memory/specs/20260624160000-define-ticket-type|Define a ticket type]]
- `F-3.2` [[_memory/specs/20260625001420-free-tickets|Free tickets]]
- `F-3.3` [[_memory/specs/20260625150117-transparent-all-inclusive-pricing|Transparent, all-inclusive pricing]]
- `F-3.4` [[_memory/specs/20260625161104-inventory-and-no-oversell|Inventory and no-oversell guarantee]]
- `F-3.5` [[_memory/specs/20260626000000-multiple-ticket-types-per-event|Multiple ticket types per event]]
- `F-3.6` [[_memory/specs/20260626160000-per-order-purchase-limit|Per-order purchase limit]]
- `F-3.7` [[_memory/specs/20260626120000-discount-codes|Discount codes]]
- `F-3.8` [[_memory/specs/20260627100000-scheduled-on-sale-window|Scheduled on-sale window]]

### EP-4 - Event Discovery & Access

- `F-4.1` [[_memory/specs/20260627120000-shareable-public-event-page|Shareable public event page]]
- `F-4.2` [[_memory/specs/20260627100000-mobile-friendly-event-page|Mobile-friendly event page]]
- `F-4.3` [[_memory/specs/20260627142118-rich-link-previews|Rich link previews]]
- `F-4.4` [[_memory/specs/20260627160000-public-event-listing|Public event listing]]
- `F-4.5` [[_memory/specs/20260627170000-search-and-filter|Search and filter]]

### EP-5 through EP-11

Specs are not present yet. Use [[_memory/source/feature-specification|feature specification]] for acceptance criteria until implementation specs are written.

## Related maps

- [[product-intent]]
- [[domain-model]]
- [[technical-architecture]]
