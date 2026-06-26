namespace EventHub.Application.Common;

public sealed record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount);
