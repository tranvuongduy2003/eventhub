namespace EventHub.Contracts.Events;

public sealed record RescheduleOccurrenceResponse(
    int OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address,
    DateTimeOffset UpdatedAt);
