using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Infrastructure.Messaging;

public sealed class NoOpIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
        where TIntegrationEvent : class =>
        Task.CompletedTask;
}
