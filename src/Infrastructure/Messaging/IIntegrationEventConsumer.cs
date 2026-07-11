namespace EventHub.Infrastructure.Messaging;

public interface IIntegrationEventConsumer
{
    Task ConsumeAsync(object integrationEvent, CancellationToken cancellationToken);
}

public interface IIntegrationEventConsumer<in TIntegrationEvent> : IIntegrationEventConsumer
    where TIntegrationEvent : class
{
    Task ConsumeAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

public abstract class IntegrationEventConsumer<TIntegrationEvent> :
    IIntegrationEventConsumer<TIntegrationEvent>
    where TIntegrationEvent : class
{
    public abstract Task ConsumeAsync(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);

    public Task ConsumeAsync(object integrationEvent, CancellationToken cancellationToken) =>
        ConsumeAsync((TIntegrationEvent)integrationEvent, cancellationToken);
}
