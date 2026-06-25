using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Events.Commands;
using EventHub.Application.Events.Queries;
using EventHub.Contracts.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class EventsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/events", CreateDraftEvent)
            .WithName("CreateDraftEvent")
            .WithTags("Events")
            .RequireAuthorization()
            .RequireCompleteJsonBody<CreateDraftEventRequest>()
            .Accepts<CreateDraftEventRequest>("application/json")
            .Produces<DraftEventResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}", GetEventDetails)
            .WithName("GetEventDetails")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<EventDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPut("/api/events/{eventId}", EditEventDetails)
            .WithName("EditEventDetails")
            .WithTags("Events")
            .RequireAuthorization()
            .RequireCompleteJsonBody<EditEventDetailsRequest>()
            .Accepts<EditEventDetailsRequest>("application/json")
            .Produces<EventDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/publish", PublishEvent)
            .WithName("PublishEvent")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<PublishEventResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/close", CloseEvent)
            .WithName("CloseEvent")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<CloseEventResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPost("/api/events/{eventId}/cancel", CancelEvent)
            .WithName("CancelEvent")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<CancelEventResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/public", GetPublicEvent)
            .WithName("GetPublicEvent")
            .WithTags("Events")
            .AllowAnonymous()
            .Produces<PublicEventResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost("/api/events/{eventId}/duplicate", DuplicateEvent)
            .WithName("DuplicateEvent")
            .WithTags("Events")
            .RequireAuthorization()
            .Produces<DuplicateEventResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreateDraftEvent(
        CreateDraftEventRequest request,
        ISender sender)
    {
        var command = new CreateDraftEventCommand(
            request.Title,
            request.StartsAt,
            request.EndsAt,
            request.TimeZoneId,
            request.PhysicalAddress,
            request.IsOnline);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var draftEvent = result.Value!;

        return Results.Json(
            new DraftEventResponse(
                draftEvent.Status,
                draftEvent.CreatedAt),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetEventDetails(
        int eventId,
        ISender sender)
    {
        var query = new GetEventDetailsQuery(eventId);

        var result = await sender.Send(query);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPublicEvent(
        int eventId,
        ISender sender)
    {
        var query = new GetPublicEventQuery(eventId);

        var result = await sender.Send(query);

        return result.ToHttpResult();
    }

    private static async Task<IResult> EditEventDetails(
        int eventId,
        EditEventDetailsRequest request,
        ISender sender)
    {
        var command = new EditEventDetailsCommand(
            eventId,
            request.Title,
            request.StartsAt,
            request.EndsAt,
            request.TimeZoneId,
            request.PhysicalAddress,
            request.IsOnline,
            request.Description);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var editResult = result.Value!;

        return Results.Ok(new EventDetailsResponse(
            eventId,
            request.Title,
            request.Description,
            request.StartsAt,
            request.EndsAt,
            request.TimeZoneId,
            request.PhysicalAddress,
            request.IsOnline,
            editResult.Status,
            editResult.UpdatedAt));
    }

    private static async Task<IResult> PublishEvent(
        int eventId,
        ISender sender)
    {
        var command = new PublishEventCommand(eventId);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var publishResult = result.Value!;

        return Results.Ok(new PublishEventResponse(
            publishResult.Status,
            publishResult.Slug,
            publishResult.UpdatedAt));
    }

    private static async Task<IResult> CloseEvent(
        int eventId,
        ISender sender)
    {
        var command = new CloseEventCommand(eventId);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var closeResult = result.Value!;

        return Results.Ok(new CloseEventResponse(
            closeResult.Status,
            closeResult.UpdatedAt));
    }

    private static async Task<IResult> CancelEvent(
        int eventId,
        ISender sender)
    {
        var command = new CancelEventCommand(eventId);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var cancelResult = result.Value!;

        return Results.Ok(new CancelEventResponse(
            cancelResult.Status,
            cancelResult.CancelledAt,
            cancelResult.UpdatedAt));
    }

    private static async Task<IResult> DuplicateEvent(
        int eventId,
        ISender sender)
    {
        var command = new DuplicateEventCommand(eventId);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var duplicateResult = result.Value!;

        return Results.Json(
            new DuplicateEventResponse(
                duplicateResult.Status,
                duplicateResult.CreatedAt),
            statusCode: StatusCodes.Status201Created);
    }
}
