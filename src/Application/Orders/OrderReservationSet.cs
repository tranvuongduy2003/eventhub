using EventHub.Domain.Events;
using EventHub.Domain.Orders;

namespace EventHub.Application.Orders;

internal static class OrderReservationSet
{
    public static List<ReservationId> GetLiveReservationIds(Event eventAggregate, OrderId orderId) =>
        eventAggregate.Reservations
            .Where(reservation => reservation.OrderId == orderId)
            .Select(reservation => reservation.Id)
            .ToList();
}
