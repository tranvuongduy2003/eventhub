namespace EventHub.Contracts.Orders;

public sealed record OrderLineResponse(
    int OrderLineId,
    int TicketTypeId,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);
