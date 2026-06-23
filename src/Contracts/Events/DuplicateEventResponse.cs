namespace EventHub.Contracts.Events;

public sealed record DuplicateEventResponse(string Status, DateTimeOffset CreatedAt);
