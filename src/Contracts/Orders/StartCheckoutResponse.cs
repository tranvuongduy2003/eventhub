namespace EventHub.Contracts.Orders;

public sealed record StartCheckoutResponse(
    int EventId,
    string EventSlug,
    string EventTitle,
    decimal TotalAmount,
    string TotalCurrency,
    List<StartCheckoutLineResponse> Lines);

public sealed record StartCheckoutLineResponse(
    int TicketTypeId,
    string TicketTypeName,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);
