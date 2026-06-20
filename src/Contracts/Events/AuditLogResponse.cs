namespace EventHub.Contracts.Events;

public sealed record AuditLogResponse(
    IReadOnlyList<AuditLogEntryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
