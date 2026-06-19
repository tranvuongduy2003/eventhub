namespace EventHub.Contracts.Events;

public sealed record DraftEventResponse(
    int EventId,
    string Status,
    DateTimeOffset CreatedAt);
