namespace EventHub.Contracts.Orders;

public sealed record StartCheckoutRequest(
    List<StartCheckoutLineRequest> Lines);

public sealed record StartCheckoutLineRequest(
    int TicketTypeId,
    int Quantity);
