namespace EventHub.Contracts.Events;

public sealed record OccurrenceResponse(
    int OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
