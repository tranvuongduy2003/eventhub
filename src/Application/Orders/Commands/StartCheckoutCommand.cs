using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Orders.Commands;

public sealed record StartCheckoutCommand(
    string Slug,
    List<StartCheckoutLineRequest> Lines)
    : ICommand<StartCheckoutResult>;

public sealed record StartCheckoutLineRequest(
    int TicketTypeId,
    int Quantity);

public sealed record StartCheckoutResult(
    int EventId,
    string EventSlug,
    string EventTitle,
    decimal TotalAmount,
    string TotalCurrency,
    List<StartCheckoutLineResult> Lines);

public sealed record StartCheckoutLineResult(
    int TicketTypeId,
    string TicketTypeName,
    int Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal LineTotalAmount,
    string LineTotalCurrency);
