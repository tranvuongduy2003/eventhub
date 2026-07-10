using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Tickets;
using EventHub.Application.Tickets.Commands;
using EventHub.Application.Tickets.Queries;
using EventHub.Contracts.Tickets;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class TicketsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/orders/{orderId}/tickets", GetOrderTickets)
            .WithName("GetOrderTickets")
            .WithTags("Tickets")
            .Produces<OrderTicketsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/orders/{orderId}/tickets/resend", ResendTickets)
            .WithName("ResendTickets")
            .WithTags("Tickets")
            .RequireCompleteJsonBody<ResendTicketsRequest>()
            .Accepts<ResendTicketsRequest>("application/json")
            .Produces<ResendTicketsResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/me/tickets", GetMyTickets)
            .WithName("GetMyTickets")
            .WithTags("Tickets")
            .RequireAuthorization()
            .Produces<MyTicketsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetOrderTickets(int orderId, ISender sender)
    {
        var result = await sender.Send(new GetOrderTicketsQuery(orderId));
        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var tickets = result.Value!;
        return Results.Ok(new OrderTicketsResponse(
            tickets.OrderId,
            tickets.OrderStatus,
            tickets.Tickets.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> ResendTickets(
        int orderId,
        ResendTicketsRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ResendTicketsCommand(orderId, request.Email));
        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Json(
            new ResendTicketsResponse(result.Value!.Accepted),
            statusCode: StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> GetMyTickets(ISender sender)
    {
        var result = await sender.Send(new GetMyTicketsQuery());
        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.Ok(new MyTicketsResponse(result.Value!.Select(ToResponse).ToList()));
    }

    private static TicketResponse ToResponse(TicketResult ticket) =>
        new(
            ticket.TicketId,
            ticket.EventId,
            ticket.EventTitle,
            ticket.EventStartsAt,
            ticket.EventEndsAt,
            ticket.EventTimeZoneId,
            ticket.EventLocation,
            ticket.EventIsOnline,
            ticket.OrderId,
            ticket.TicketTypeId,
            ticket.TicketTypeName,
            ticket.Code,
            ticket.HolderName,
            ticket.HolderEmail,
            ticket.Status,
            ticket.IssuedAt);
}
