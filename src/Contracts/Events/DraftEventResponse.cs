namespace EventHub.Contracts.Events;

public sealed record DraftEventResponse(
    string Status,
    DateTimeOffset CreatedAt);
