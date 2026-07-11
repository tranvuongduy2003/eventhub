using System.Threading.Channels;

namespace EventHub.Infrastructure.Messaging;

public sealed class ChannelIntegrationEventQueue
{
    private readonly Channel<QueuedIntegrationEvent> _channel =
        Channel.CreateUnbounded<QueuedIntegrationEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
        where TIntegrationEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return _channel.Writer.WriteAsync(
            new QueuedIntegrationEvent(typeof(TIntegrationEvent), integrationEvent),
            cancellationToken);
    }

    public IAsyncEnumerable<QueuedIntegrationEvent> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    public bool TryRead(out QueuedIntegrationEvent? integrationEvent) =>
        _channel.Reader.TryRead(out integrationEvent);
}
