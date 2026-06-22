namespace EventHub.Contracts.Events;

public sealed record EditEventDetailsRequest(
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline,
    string? Description);
