using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Infrastructure.Messaging;

public sealed class ChannelIntegrationEventConsumerService(
    ChannelIntegrationEventQueue integrationEventQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ChannelIntegrationEventConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var integrationEvent in integrationEventQueue.ReadAllAsync(stoppingToken))
            {
                await ConsumeAsync(integrationEvent, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ConsumeAsync(QueuedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var consumerType = typeof(IIntegrationEventConsumer<>).MakeGenericType(integrationEvent.EventType);
            var consumer = scope.ServiceProvider.GetService(consumerType);

            if (consumer is null)
            {
                logger.LogWarning(
                    "No Channel consumer is registered for integration event {IntegrationEventType}",
                    integrationEvent.EventType.FullName);
                return;
            }

            await ((IIntegrationEventConsumer)consumer).ConsumeAsync(
                integrationEvent.Payload,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Channel consumer failed for integration event {IntegrationEventType}",
                integrationEvent.EventType.FullName);
        }
    }
}
