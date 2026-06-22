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
}
