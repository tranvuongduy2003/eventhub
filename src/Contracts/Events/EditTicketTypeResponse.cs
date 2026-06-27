namespace EventHub.Contracts.Events;

public sealed record EditTicketTypeResponse(
    int TicketTypeId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int? MaxPerOrder,
    DateTimeOffset? SalesWindowStart,
    DateTimeOffset? SalesWindowEnd,
    int Sold,
    int Reserved,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
