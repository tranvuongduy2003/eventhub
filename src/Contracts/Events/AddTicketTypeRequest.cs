namespace EventHub.Contracts.Events;

public sealed record AddTicketTypeRequest(
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int? MaxPerOrder,
    DateTimeOffset? SalesWindowStart,
    DateTimeOffset? SalesWindowEnd);
