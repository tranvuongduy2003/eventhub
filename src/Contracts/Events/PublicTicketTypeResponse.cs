namespace EventHub.Contracts.Events;

public sealed record PublicTicketTypeResponse(
    int TicketTypeId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int Sold,
    int Reserved,
    bool IsSoldOut);
