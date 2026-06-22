namespace EventHub.Contracts.Events;

public sealed record PublishEventResponse(string Status, string Slug, DateTimeOffset UpdatedAt);
