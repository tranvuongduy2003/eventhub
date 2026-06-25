namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class ReservationRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int TicketTypeId { get; set; }

    public int Quantity { get; set; }

    public int OrderId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
