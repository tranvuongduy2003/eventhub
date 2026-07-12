using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Realtime;

internal sealed class CollectSalesInventoryUpdateOnInventoryReservedHandler(
    IPendingRealtimeSalesInventoryUpdateCollector collector)
    : IDomainEventHandler<InventoryReservedEvent>
{
    public Task Handle(InventoryReservedEvent domainEvent, CancellationToken cancellationToken)
    {
        collector.Add(domainEvent.EventId);
        return Task.CompletedTask;
    }
}

internal sealed class CollectSalesInventoryUpdateOnReservationCommittedHandler(
    IPendingRealtimeSalesInventoryUpdateCollector collector)
    : IDomainEventHandler<ReservationCommittedEvent>
{
    public Task Handle(ReservationCommittedEvent domainEvent, CancellationToken cancellationToken)
    {
        collector.Add(domainEvent.EventId);
        return Task.CompletedTask;
    }
}

internal sealed class CollectSalesInventoryUpdateOnReservationReleasedHandler(
    IPendingRealtimeSalesInventoryUpdateCollector collector)
    : IDomainEventHandler<ReservationReleasedEvent>
{
    public Task Handle(ReservationReleasedEvent domainEvent, CancellationToken cancellationToken)
    {
        collector.Add(domainEvent.EventId);
        return Task.CompletedTask;
    }
}

internal sealed class CollectSalesInventoryUpdateOnInventoryReturnedToPoolHandler(
    IPendingRealtimeSalesInventoryUpdateCollector collector)
    : IDomainEventHandler<InventoryReturnedToPoolEvent>
{
    public Task Handle(InventoryReturnedToPoolEvent domainEvent, CancellationToken cancellationToken)
    {
        collector.Add(domainEvent.EventId);
        return Task.CompletedTask;
    }
}
