using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Orders.Commands;
using EventHub.Domain.Orders;
using MediatR;

namespace EventHub.Application.Orders.EventHandlers;

internal sealed class ReleaseReservationOnOrderExpiredHandler(
    ISender sender)
    : IDomainEventHandler<OrderExpiredEvent>
{
    public async Task Handle(OrderExpiredEvent domainEvent, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReconcileOrderReservationsCommand(
                domainEvent.OrderId,
                OrderReservationTransition.Release),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Could not release order reservations.");
        }
    }
}
