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

        endpoints.MapPost("/api/orders/{orderId}/tickets/{ticketId}/transfer", TransferTicket)
            .WithName("TransferTicket")
            .WithTags("Tickets")
            .RequireCompleteJsonBody<TransferTicketRequest>()
            .Accepts<TransferTicketRequest>("application/json")
            .Produces<TicketResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/orders/{orderId}/tickets/{ticketId}/return", ReturnTicket)
            .WithName("ReturnTicket")
            .WithTags("Tickets")
            .Produces<ReturnTicketResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/me/tickets", GetMyTickets)
            .WithName("GetMyTickets")
            .WithTags("Tickets")
            .RequireAuthorization()
            .Produces<MyTicketsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/check-ins/scan", CheckInByCode)
            .WithName("CheckInTicketByCode")
            .WithTags("Check-ins")
            .RequireAuthorization()
            .RequireCompleteJsonBody<CheckInTicketRequest>()
            .Accepts<CheckInTicketRequest>("application/json")
            .Produces<CheckInTicketResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/check-ins/sync", BatchCheckIn)
            .WithName("BatchCheckInTickets")
            .WithTags("Check-ins")
            .RequireAuthorization()
            .RequireCompleteJsonBody<BatchCheckInTicketsRequest>()
            .Accepts<BatchCheckInTicketsRequest>("application/json")
            .Produces<BatchCheckInTicketsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/check-ins/tickets", SearchCheckInTickets)
            .WithName("SearchCheckInTickets")
            .WithTags("Check-ins")
            .RequireAuthorization()
            .Produces<SearchCheckInTicketsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/check-ins/tickets/{ticketId}", CheckInByTicketId)
            .WithName("CheckInTicketById")
            .WithTags("Check-ins")
            .RequireAuthorization()
            .Produces<CheckInTicketResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/check-ins/counts", GetDoorCounts)
            .WithName("GetDoorCounts")
            .WithTags("Check-ins")
            .RequireAuthorization()
            .Produces<DoorCountsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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

    private static async Task<IResult> TransferTicket(
        int orderId,
        int ticketId,
        TransferTicketRequest request,
        ISender sender)
    {
        var result = await sender.Send(new TransferTicketCommand(
            orderId,
            ticketId,
            request.RecipientName,
            request.RecipientEmail));

        return result.ToHttpResult(ticket => Results.Ok(ToResponse(ticket)));
    }

    private static async Task<IResult> ReturnTicket(
        int orderId,
        int ticketId,
        ISender sender)
    {
        var result = await sender.Send(new ReturnTicketCommand(orderId, ticketId));

        return result.ToHttpResult(returned => Results.Ok(new ReturnTicketResponse(
            returned.TicketId,
            returned.OrderId,
            returned.EventId,
            returned.TicketStatus,
            returned.OrderStatus,
            returned.PaymentStatus)));
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

    private static async Task<IResult> CheckInByCode(
        int eventId,
        CheckInTicketRequest request,
        ISender sender)
    {
        var result = await sender.Send(new CheckInTicketByCodeCommand(eventId, request.Code));

        return result.ToHttpResult(ticket => Results.Ok(ToResponse(ticket)));
    }

    private static async Task<IResult> BatchCheckIn(
        int eventId,
        BatchCheckInTicketsRequest request,
        ISender sender)
    {
        var result = await sender.Send(new BatchCheckInTicketsCommand(
            eventId,
            request.Tickets
                .Select(ticket => new Application.Tickets.Commands.BatchCheckInTicketRequest(
                    ticket.ClientScanId,
                    ticket.Code,
                    ticket.ScannedAt))
                .ToList()));

        return result.ToHttpResult(batch => Results.Ok(new BatchCheckInTicketsResponse(
            batch.Results.Select(item => new BatchCheckInTicketResponse(
                item.ClientScanId,
                item.Code,
                item.Accepted,
                item.Status,
                item.Reason,
                item.Ticket is null ? null : ToResponse(item.Ticket))).ToList())));
    }

    private static async Task<IResult> SearchCheckInTickets(
        int eventId,
        string query,
        ISender sender)
    {
        var result = await sender.Send(new SearchCheckInTicketsQuery(eventId, query));

        return result.ToHttpResult(tickets => Results.Ok(new SearchCheckInTicketsResponse(
            tickets.Select(ToResponse).ToList())));
    }

    private static async Task<IResult> CheckInByTicketId(
        int eventId,
        int ticketId,
        ISender sender)
    {
        var result = await sender.Send(new CheckInTicketByIdCommand(eventId, ticketId));

        return result.ToHttpResult(ticket => Results.Ok(ToResponse(ticket)));
    }

    private static async Task<IResult> GetDoorCounts(int eventId, ISender sender)
    {
        var result = await sender.Send(new GetDoorCountsQuery(eventId));

        return result.ToHttpResult(counts => Results.Ok(new DoorCountsResponse(
            counts.CheckedIn,
            counts.TotalIssued)));
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

    private static CheckInTicketResponse ToResponse(CheckInTicketResult ticket) =>
        new(
            ticket.TicketId,
            ticket.EventId,
            ticket.OrderId,
            ticket.TicketTypeId,
            ticket.Code,
            ticket.HolderName,
            ticket.HolderEmail,
            ticket.Status,
            ticket.IssuedAt,
            ticket.CheckedInAt);
}
