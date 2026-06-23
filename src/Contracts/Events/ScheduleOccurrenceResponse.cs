namespace EventHub.Contracts.Events;

public sealed record ScheduleOccurrenceResponse(
    int OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address,
    DateTimeOffset CreatedAt);
