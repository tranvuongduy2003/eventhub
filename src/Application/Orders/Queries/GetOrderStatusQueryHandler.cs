using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Orders;

namespace EventHub.Application.Orders.Queries;

public sealed class GetOrderStatusQueryHandler(
    IOrderRepository orderRepository,
    IDiscountCodeRepository discountCodeRepository)
    : QueryHandler<GetOrderStatusQuery, GetOrderStatusResult>
{
    public override async Task<Result<GetOrderStatusResult>> Handle(
        GetOrderStatusQuery query,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(query.OrderId);

        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return Error.NotFound("ORDER_NOT_FOUND", "The order was not found.");
        }

        // Resolve discount code string from ID (if any)
        string? discountCode = null;
        if (order.DiscountCodeId.HasValue)
        {
            var dc = await discountCodeRepository.GetByIdAsync(order.DiscountCodeId.Value, cancellationToken);
            discountCode = dc?.Code;
        }

        return new GetOrderStatusResult(
            order.Id.Value,
            order.Status.ToString().ToLowerInvariant(),
            order.Total.Amount,
            order.Total.Currency,
            order.PaymentId,
            order.PlacedAt,
            order.ConfirmedAt,
            order.Lines.Select(l => new GetOrderStatusLineResult(
                l.Id.Value,
                l.TicketTypeId.Value,
                l.Quantity,
                l.UnitPriceSnapshot.Amount,
                l.UnitPriceSnapshot.Currency,
                l.LineTotal.Amount,
                l.LineTotal.Currency)).ToList(),
            discountCode,
            order.DiscountAmount?.Amount);
    }
}
