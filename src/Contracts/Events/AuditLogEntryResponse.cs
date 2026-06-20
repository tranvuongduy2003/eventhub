namespace EventHub.Contracts.Events;

public sealed record AuditLogEntryResponse(
    int Id,
    string ActorName,
    string TargetName,
    string Action,
    string? OldRole,
    string? NewRole,
    DateTimeOffset OccurredAt);
