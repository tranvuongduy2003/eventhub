using EventHub.Domain.Abstractions;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Events;

public sealed class Reservation : Entity<ReservationId>
{
    private Reservation()
    {
    }

    public TicketTypeId TicketTypeId { get; private set; }

    public int Quantity { get; private set; }

    public OrderId OrderId { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Reservation Create(
        ReservationId id,
        TicketTypeId ticketTypeId,
        int quantity,
        OrderId orderId,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = id,
            TicketTypeId = ticketTypeId,
            Quantity = quantity,
            OrderId = orderId,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
        };

    public static Reservation FromPersistence(
        ReservationId id,
        TicketTypeId ticketTypeId,
        int quantity,
        OrderId orderId,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = id,
            TicketTypeId = ticketTypeId,
            Quantity = quantity,
            OrderId = orderId,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
        };
}
