namespace EventHub.Contracts.Events;

public sealed record EventDetailsResponse(
    int EventId,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline,
    string Status,
    DateTimeOffset UpdatedAt);
