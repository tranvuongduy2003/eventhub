---
doc_schema: eventhub-doc-v1
doc_kind: report
doc_id: eventhub.pending-features
title: Pending and Not-Confirmed Features
status: active
last_updated: 2026-07-12
owner: builder
language: en
source_documents:
  - docs/features.md
  - docs/specs/
---

# Pending and Not-Confirmed Features

This report lists features that are not marked `delivery implemented` in `docs/features.md`, cross-checked against implementation specs in `docs/specs/`.

## Summary

| Delivery status | Count |
| --- | ---: |
| not-confirmed | 0 |
| planned | 0 |
| **Total pending / unimplemented** | **0** |

## Pending Feature List

No pending features remain as of 2026-07-12. The previous pending set was reconciled by:

- existing implementation evidence for the EventHub account, event, ticketing, discovery, purchase, payment, delivery, check-in, audience/results, and transfer spine;
- `docs/specs/20260712190000-pending-feature-completion.md` for the final planned completion slice;
- backend/API implementation for refund-on-cancellation, return-to-pool, offline batch check-in sync, live check-in updates, and low-stock/sold-out realtime indicators;
- OpenAPI contract export and web type/build verification.

## Verification Evidence

- `dotnet test tests/Api.IntegrationTests/EventHub.Api.IntegrationTests.csproj -c Release` passed with 303 tests.
- `yarn --cwd web api:verify` passed.
- `yarn --cwd web build` passed.

