namespace EventHub.Contracts.Events;

public sealed record RescheduleOccurrenceRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address);
