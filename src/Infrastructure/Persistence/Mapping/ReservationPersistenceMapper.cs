using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class ReservationPersistenceMapper
{
    public static ReservationRecord ToRecord(Reservation domain, int eventId) =>
        new()
        {
            Id = domain.Id.Value,
            EventId = eventId,
            TicketTypeId = domain.TicketTypeId.Value,
            Quantity = domain.Quantity,
            OrderId = domain.OrderId.Value,
            ExpiresAt = domain.ExpiresAt,
            CreatedAt = domain.CreatedAt,
        };

    public static Reservation ToDomain(ReservationRecord record) =>
        Reservation.FromPersistence(
            ReservationId.From(record.Id),
            TicketTypeId.From(record.TicketTypeId),
            record.Quantity,
            OrderId.From(record.OrderId),
            record.ExpiresAt,
            record.CreatedAt);
}
