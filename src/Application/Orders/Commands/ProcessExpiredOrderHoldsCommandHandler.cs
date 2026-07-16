using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;

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

        foreach (var ordersByEvent in expiredOrders.GroupBy(order => order.EventId))
        {
            var eventAggregate = await eventRepository.GetByIdAsync(ordersByEvent.Key, cancellationToken);
            if (eventAggregate is null)
            {
                continue;
            }

            var eventChanged = false;

            foreach (var order in ordersByEvent)
            {
                var reservationIds = OrderReservationSet.GetLiveReservationIds(eventAggregate, order.Id);

                order.Expire(now);

                foreach (var reservationId in reservationIds)
                {
                    eventAggregate.ReleaseReservation(reservationId, now);
                    releasedReservationCount++;
                }

                order.ClearReservationId();
                await orderRepository.Update(order, cancellationToken);

                pendingDomainEventsCollector.AddRange(order.DomainEvents);
                order.ClearDomainEvents();

                expiredOrderCount++;
                eventChanged = true;
            }

            if (!eventChanged)
            {
                continue;
            }

            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();
        }

        return new ProcessExpiredOrderHoldsResult(expiredOrderCount, releasedReservationCount);
    }
}
