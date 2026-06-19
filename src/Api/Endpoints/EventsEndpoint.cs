using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Events.Commands;
using EventHub.Contracts.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class EventsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints) =>
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

        return Results.Created(
            $"/api/events/{draftEvent.EventId:D}",
            new DraftEventResponse(
                draftEvent.EventId,
                draftEvent.Status,
                draftEvent.CreatedAt));
    }
}
