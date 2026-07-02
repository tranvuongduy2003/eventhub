---
title: Product Intent MOC
type: moc
status: active
tags:
  - moc/product
  - product
---

# Product Intent MOC

Authoritative source: [[_memory/source/product-requirements|product requirements]].

## Core idea

EventHub is a simple, fair event management and ticketing platform for small events. It is a personal project optimized for a complete, maintainable end-to-end product rather than commercial breadth.

## Product anchors

- Vision: simple, fair, and well-built ticketing for small organizers and attendees.
- North Star: events successfully run end-to-end.
- Main personas: organizers (`PER-O1`, `PER-O2`) and attendees (`PER-A1`, `PER-A2`).
- Quality guardrails: simplicity, transparent pricing, fairness, mobile-friendly purchase, no oversell, responsible handling of data and money.

## Decisions

- [[_memory/glossary/decision-log#DEC-1 - monetization|DEC-1]] - no platform fee at launch; paid events use trusted provider.
- [[_memory/glossary/decision-log#DEC-2 - resale and transfer|DEC-2]] - face-value transfer only; no paid secondary marketplace.
- [[_memory/glossary/decision-log#DEC-3 - first release|DEC-3]] - small MVP proving create -> sell -> deliver -> check-in.
- [[_memory/glossary/decision-log#DEC-4 - first organizers|DEC-4]] - self-service; builder's own or friends' events.
- [[_memory/glossary/decision-log#DEC-5 - timeline|DEC-5]] - no fixed deadlines; milestone-based.

## Related maps

- [[feature-roadmap]] - what the product does.
- [[domain-model]] - the business model behind behavior.
- [[technical-architecture]] - how it is built.
