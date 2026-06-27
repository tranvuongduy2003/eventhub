namespace EventHub.Contracts.Events;

public sealed record PublicTicketTypeResponse(
    int TicketTypeId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int? MaxPerOrder,
    int Sold,
    int Reserved,
    int Available,
    bool IsSoldOut,
    DateTimeOffset? SalesWindowStart,
    DateTimeOffset? SalesWindowEnd,
    string? SalesWindowStatus);
