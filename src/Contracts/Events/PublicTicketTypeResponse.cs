namespace EventHub.Contracts.Events;

public sealed record PublicTicketTypeResponse(
    int TicketTypeId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int? MaxPerOrder,
    bool IsPurchasable,
    string AvailabilityState,
    string AvailabilityReason,
    DateTimeOffset? SalesWindowStart,
    DateTimeOffset? SalesWindowEnd,
    string? SalesWindowStatus);
