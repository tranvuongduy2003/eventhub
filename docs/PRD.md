# Product Requirements Document

**Project:** Clean Architecture + CQRS + DDD Boilerplate  
**Status:** Template  
**Last Updated:** June 7, 2026

---

## Purpose

This repository is a **.NET boilerplate**, not a product. It demonstrates a local-first backend template using:

- **Clean Architecture** (Domain → Application → Infrastructure; Api as composition root)
- **CQRS** with MediatR (commands, queries, pipeline behaviors)
- **Domain-Driven Design** (aggregates, value objects, domain events)

Use it as a starting point for new services. Replace the sample `User` bounded context with your own domain.

## What is included

| Area | Sample implementation |
|------|------------------------|
| Domain | `User` aggregate with registration invariants |
| Application | Register, login, logout commands; MediatR pipeline |
| Infrastructure | EF Core + PostgreSQL, Redis session cache |
| Api | Minimal REST endpoints, cookie session auth, OpenAPI |
| Local run | .NET Aspire AppHost (PostgreSQL + Redis + Api) |

## What is out of scope

- Production deployment, multi-tenancy, or specific business features
- Frontend (optional; add your own client)
- Message brokers, outbox, or horizontal scaling patterns

## Docs map

| Document | Use for |
|----------|---------|
| [`CONSTITUTION.md`](CONSTITUTION.md) | Non-negotiable invariants |
| [`TECHNICAL.md`](TECHNICAL.md) | Architecture, layers, CQRS, API, persistence |
| [`memory/current-status.md`](memory/current-status.md) | Active work and session checklist |
| [`memory/decisions.md`](memory/decisions.md) | Architecture decision records |
| [`memory/known-issues.md`](memory/known-issues.md) | Known bugs and workarounds |
