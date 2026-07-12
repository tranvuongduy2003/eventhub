using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Realtime;

internal sealed class CollectCheckInUpdateOnTicketCheckedInHandler(
    IPendingRealtimeCheckInUpdateCollector collector)
    : IDomainEventHandler<TicketCheckedInEvent>
{
    public Task Handle(TicketCheckedInEvent domainEvent, CancellationToken cancellationToken)
    {
        collector.Add(domainEvent.EventId);
        return Task.CompletedTask;
    }
}
