---
title: Ubiquitous Language
type: glossary
status: active
tags:
  - glossary/domain
  - domain
---

# Ubiquitous Language

Authoritative source: [[_memory/source/domain-model-specification#1. Ubiquitous Language|domain model specification section 1]].

Use these terms consistently in specs, code, tests, and agent conversations.

| Term | Meaning | Owner |
|---|---|---|
| Organizer | Person or account that creates and runs events. | Identity & Access |
| Attendee | Person who buys or holds a ticket and attends. | Identity & Access |
| Event | Happening offered by an organizer with schedule, location, and ticket types. | Event Management |
| Ticket Type | Sellable category within an event. | Event Management |
| Inventory / Availability | `Available = Capacity - Reserved - Sold`. | Event Management |
| Reservation | Time-limited hold on inventory while an order is being paid. | Event Management |
| Order | Attendee purchase of one or more ticket types for an event. | Sales |
| Price Snapshot | Order-line price captured at placement so later price changes do not alter the order. | Sales |
| Payment | Attempt to settle an order through the external provider. | Payments |
| Ticket | Issued admission with a unique scannable code. | Ticketing |
| Check-in | Validating a ticket at the door and admitting exactly once. | Ticketing |
| Transfer | Reassigning a ticket to another contact at face value. | Ticketing |
| Return-to-pool | Returning a ticket so it can be resold at face value. | Ticketing / Event Management |
| Notification | Email sent from domain events or scheduled actions. | Notifications |
| Attendee List / Results | Read models showing audience and event performance. | Reporting & Audience |

For the full table and tactical model, read [[_memory/source/domain-model-specification|domain model specification]] before changing domain code.
