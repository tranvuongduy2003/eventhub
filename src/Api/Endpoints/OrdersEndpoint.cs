using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Orders.Commands;
using EventHub.Application.Orders.Queries;
using EventHub.Contracts.Orders;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class OrdersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/events/{eventId}/orders", PlaceOrder)
            .WithName("PlaceOrder")
            .WithTags("Orders")
            .RequireCompleteJsonBody<PlaceOrderRequest>()
            .Accepts<PlaceOrderRequest>("application/json")
            .Produces<PlaceOrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/orders/{orderId}", GetOrderStatus)
            .WithName("GetOrderStatus")
            .WithTags("Orders")
            .Produces<OrderStatusResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> PlaceOrder(
        int eventId,
        PlaceOrderRequest request,
        ISender sender)
    {
        var command = new PlaceOrderCommand(
            eventId,
            request.ContactName,
            request.ContactEmail,
            request.Lines.Select(l => new Application.Orders.Commands.PlaceOrderLineRequest(
                l.TicketTypeId,
                l.Quantity)).ToList(),
            request.DiscountCode);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var order = result.Value!;

        return Results.Json(
            new PlaceOrderResponse(
                order.OrderId,
                order.Status,
                order.TotalAmount,
                order.TotalCurrency,
                order.PaymentId,
                order.PlacedAt,
                order.ConfirmedAt,
                order.Lines.Select(l => new OrderLineResponse(
                    l.OrderLineId,
                    l.TicketTypeId,
                    l.Quantity,
                    l.UnitPriceAmount,
                    l.UnitPriceCurrency,
                    l.LineTotalAmount,
                    l.LineTotalCurrency)).ToList(),
                order.DiscountCode,
                order.DiscountAmount),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetOrderStatus(
        int orderId,
        ISender sender)
    {
        var query = new GetOrderStatusQuery(orderId);
        var result = await sender.Send(query);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var order = result.Value!;

        return Results.Ok(
            new OrderStatusResponse(
                order.OrderId,
                order.Status,
                order.TotalAmount,
                order.TotalCurrency,
                order.PaymentId,
                order.PlacedAt,
                order.ConfirmedAt,
                order.Lines.Select(l => new OrderLineResponse(
                    l.OrderLineId,
                    l.TicketTypeId,
                    l.Quantity,
                    l.UnitPriceAmount,
                    l.UnitPriceCurrency,
                    l.LineTotalAmount,
                    l.LineTotalCurrency)).ToList(),
                order.DiscountCode,
                order.DiscountAmount));
    }
}
