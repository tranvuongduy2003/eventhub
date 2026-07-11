namespace EventHub.Infrastructure.Messaging;

public sealed record QueuedIntegrationEvent(Type EventType, object Payload);
