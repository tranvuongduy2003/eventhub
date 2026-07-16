using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Orders.Commands;
using EventHub.Domain.Orders;
using MediatR;

namespace EventHub.Application.Orders.EventHandlers;

internal sealed class CommitReservationOnOrderConfirmedHandler(
    ISender sender)
    : IDomainEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent domainEvent, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReconcileOrderReservationsCommand(
                domainEvent.OrderId,
                OrderReservationTransition.Commit),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error?.Message ?? "Could not commit order reservations.");
        }
    }
}
