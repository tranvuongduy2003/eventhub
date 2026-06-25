using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Domain.Orders;

namespace EventHub.Application.Orders.EventHandlers;

internal sealed class CommitReservationOnOrderConfirmedHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    IClock clock)
    : IDomainEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent domainEvent, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null || order.ReservationId is null)
        {
            return;
        }

        var eventAggregate = await eventRepository.GetByIdAsync(order.EventId, cancellationToken);
        if (eventAggregate is null)
        {
            return;
        }

        eventAggregate.CommitReservation(order.ReservationId.Value, clock.UtcNow);

        await eventRepository.Update(eventAggregate, cancellationToken);
    }
}
