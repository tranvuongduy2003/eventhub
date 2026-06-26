namespace EventHub.Contracts.Events;

public sealed record AddTicketTypeResponse(
    int TicketTypeId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int? MaxPerOrder,
    int Sold,
    int Reserved,
    DateTimeOffset CreatedAt);
