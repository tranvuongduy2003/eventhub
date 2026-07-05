using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Orders.Commands;

public sealed class ProcessExpiredOrderHoldsCommandHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<ProcessExpiredOrderHoldsCommand, ProcessExpiredOrderHoldsResult>
{
    public override async Task<Result<ProcessExpiredOrderHoldsResult>> Handle(
        ProcessExpiredOrderHoldsCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var expiredOrders = await orderRepository.GetPendingExpiredBeforeAsync(now, cancellationToken);

        var expiredOrderCount = 0;
        var releasedReservationCount = 0;

        foreach (var order in expiredOrders)
        {
            try
            {
                var eventAggregate = await eventRepository.GetByIdAsync(order.EventId, cancellationToken);
                if (eventAggregate is null)
                {
                    continue;
                }

                var reservations = eventAggregate.Reservations
                    .Where(reservation => reservation.OrderId == order.Id)
                    .Select(reservation => reservation.Id)
                    .ToList();

                order.Expire(now);

                foreach (var reservationId in reservations)
                {
                    eventAggregate.ReleaseReservation(reservationId, now);
                    releasedReservationCount++;
                }

                order.ClearReservationId();

                await orderRepository.Update(order, cancellationToken);
                await eventRepository.Update(eventAggregate, cancellationToken);

                pendingDomainEventsCollector.AddRange(order.DomainEvents);
                order.ClearDomainEvents();
                pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
                eventAggregate.ClearDomainEvents();

                expiredOrderCount++;
            }
            catch (BusinessRuleValidationException)
            {
                continue;
            }
        }

        return new ProcessExpiredOrderHoldsResult(expiredOrderCount, releasedReservationCount);
    }
}

