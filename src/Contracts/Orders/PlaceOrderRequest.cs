namespace EventHub.Contracts.Orders;

public sealed record PlaceOrderRequest(
    string ContactName,
    string ContactEmail,
    List<PlaceOrderLineRequest> Lines,
    string? DiscountCode = null);

public sealed record PlaceOrderLineRequest(
    int TicketTypeId,
    int Quantity);
