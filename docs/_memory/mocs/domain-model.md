---
title: Domain Model MOC
type: moc
status: active
tags:
  - moc/domain
  - domain
---

# Domain Model MOC

Authoritative source: [[_memory/source/domain-model-specification|domain model specification]].

## Bounded contexts

| ID | Context | Aggregate or shape | Notes |
|---|---|---|---|
| `BC-1` | Identity & Access | `AGG-User`, `AGG-Session` | Accounts, sessions, organizer identity |
| `BC-2` | Event Management | `AGG-Event` | Events, ticket types, lifecycle, inventory, reservations |
| `BC-3` | Sales | `AGG-Order` | Orders, holds, discounts, price snapshots |
| `BC-4` | Payments | `AGG-Payment` | External provider ACL, payment and refund status |
| `BC-5` | Ticketing | `AGG-Ticket` | Ticket issuance, check-in, transfer, returns |
| `BC-6` | Notifications | Event-driven | Email delivery; no aggregate |
| `BC-7` | Reporting & Audience | Read projections | Attendee lists and results |

## High-risk invariants

- `INV-10`: no oversell: `Reserved + Sold <= Capacity`.
- `INV-21`: a pending order must reference a live reservation.
- `INV-25`: order line prices are snapshotted at placement.
- `INV-32`: payment capture is idempotent.
- `INV-40`: a ticket code admits exactly once.
- `INV-42`: transfer is face value only.

## Event flow anchors

- Purchase path: `Event.Reserve` + `Order.Place` -> payment/free confirmation -> `OrderConfirmed` -> commit reservation -> issue tickets -> email -> check-in.
- Cancellation path: `EventCancelled` fans out to Sales, Payments, Ticketing, and Notifications.
- Reporting path: projections consume order, ticket, and check-in events.

## Related memory

- [[_memory/glossary/ubiquitous-language]]
- [[feature-roadmap]]
- [[technical-architecture]]
