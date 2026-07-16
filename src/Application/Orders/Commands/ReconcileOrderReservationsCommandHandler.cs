using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;

namespace EventHub.Application.Orders.Commands;

public sealed class ReconcileOrderReservationsCommandHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<ReconcileOrderReservationsCommand>
{
    public override async Task<Result> Handle(
        ReconcileOrderReservationsCommand command,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null || !HasExpectedStatus(order.Status, command.Transition))
        {
            return Result.Success();
        }

        var eventAggregate = await eventRepository.GetByIdAsync(order.EventId, cancellationToken);
        if (eventAggregate is null)
        {
            return Result.Success();
        }

        var reservationIds = OrderReservationSet.GetLiveReservationIds(eventAggregate, order.Id);
        if (reservationIds.Count == 0 && order.ReservationId is null)
        {
            return Result.Success();
        }

        try
        {
            var now = clock.UtcNow;
            foreach (var reservationId in reservationIds)
            {
                if (command.Transition is OrderReservationTransition.Commit)
                {
                    eventAggregate.CommitReservation(reservationId, now);
                }
                else
                {
                    eventAggregate.ReleaseReservation(reservationId, now);
                }
            }

            order.ClearReservationId();

            if (reservationIds.Count > 0)
            {
                await eventRepository.Update(eventAggregate, cancellationToken);
                pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
                eventAggregate.ClearDomainEvents();
            }

            await orderRepository.Update(order, cancellationToken);

            return Result.Success();
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }

    private static bool HasExpectedStatus(OrderStatus status, OrderReservationTransition transition) =>
        transition switch
        {
            OrderReservationTransition.Commit => status is OrderStatus.Confirmed,
            OrderReservationTransition.Release => status is OrderStatus.Expired,
            _ => false,
        };
}
