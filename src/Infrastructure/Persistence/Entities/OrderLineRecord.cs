namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class OrderLineRecord
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int TicketTypeId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceAmount { get; set; }

    public required string UnitPriceCurrency { get; set; }

    public decimal LineTotalAmount { get; set; }

    public required string LineTotalCurrency { get; set; }
}
