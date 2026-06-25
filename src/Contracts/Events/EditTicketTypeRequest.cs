namespace EventHub.Contracts.Events;

public sealed record EditTicketTypeRequest(
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity);
