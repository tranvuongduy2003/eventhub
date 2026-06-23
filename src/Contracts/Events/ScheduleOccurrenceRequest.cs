namespace EventHub.Contracts.Events;

public sealed record ScheduleOccurrenceRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address);
