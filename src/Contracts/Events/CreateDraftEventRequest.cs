namespace EventHub.Contracts.Events;

public sealed record CreateDraftEventRequest(
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline);
