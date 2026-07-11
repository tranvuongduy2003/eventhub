# Query Workflow

Use this for read paths, filters, pagination, projections, and EF translation-sensitive changes.

## Scout

```powershell
rg -n "Get.*Query|List.*Query|Search.*Query|QueryHandler" src/Application
rg -n "Get.*Async|List.*Async|PaginatedResult|AsNoTracking|Include\(" src/Infrastructure/Persistence/Repositories
rg -n "EventFilter|DatePreset|Page|PageSize|Search" src/Application src/Infrastructure tests
```

## Change Flow

1. Locate the query record, handler, repository port, repository implementation, and endpoint response.
2. Mirror nearby filtering, sorting, pagination, cancellation-token, and result-shaping patterns.
3. Prefer integration coverage when an in-memory fake would hide SQL translation or provider behavior.
4. Verify public query responses through API integration tests when the contract shape or status behavior changes.
