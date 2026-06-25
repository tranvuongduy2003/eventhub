namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class OrderRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public required string ContactName { get; set; }

    public required string ContactEmail { get; set; }

    public required string Status { get; set; }

    public decimal TotalAmount { get; set; }

    public required string TotalCurrency { get; set; }

    public int? PaymentId { get; set; }

    public int? ReservationId { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public long RowVersion { get; set; }

    public List<OrderLineRecord> Lines { get; set; } = [];
}
