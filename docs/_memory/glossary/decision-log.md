---
title: Decision Log
type: decision_index
status: active
tags:
  - decisions
  - product
---

# Decision Log

Authoritative source: [[_memory/source/product-requirements#11. Key Decisions, Assumptions & Dependencies|product requirements section 11]].

## DEC-1 - monetization

No platform fee at launch. Free events are free. Paid events use a trusted provider; EventHub holds no funds and takes no cut.

Impacts: transparent pricing, payment architecture, refund behavior, reporting revenue.

## DEC-2 - resale and transfer

Transfer is face value only. No paid secondary marketplace and no markup path.

Impacts: ticket transfer model, anti-scalping rules, future return-to-pool behavior.

## DEC-3 - first release

The first release is a small MVP that runs one small event end-to-end: create event, set up tickets, attendee buys through shared link, ticket delivered, check-in at the door.

Impacts: roadmap priority, scope control, acceptance of deferred features.

## DEC-4 - first organizers

Fully self-service. First events are the builder's own or friends' events.

Impacts: no sales workflow, no enterprise onboarding, no CRM assumptions.

## DEC-5 - timeline

No fixed deadlines. Work is milestone-based and sustainable for a personal project.

Impacts: roadmap language, no calendar commitments in specs unless explicitly added.
