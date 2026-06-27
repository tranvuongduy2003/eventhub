namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class TicketTypeRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public required string Name { get; set; }

    public decimal PriceAmount { get; set; }

    public required string PriceCurrency { get; set; }

    public int Capacity { get; set; }

    public int? MaxPerOrder { get; set; }

    public DateTimeOffset? SalesWindowStart { get; set; }

    public DateTimeOffset? SalesWindowEnd { get; set; }

    public int Sold { get; set; }

    public int Reserved { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
