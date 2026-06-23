using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Events.Commands;
using EventHub.Application.Events.Queries;
using EventHub.Contracts.Events;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class OccurrencesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/events/{eventId}/occurrences", ScheduleOccurrence)
            .WithName("ScheduleOccurrence")
            .WithTags("Occurrences")
            .RequireAuthorization()
            .RequireCompleteJsonBody<ScheduleOccurrenceRequest>()
            .Accepts<ScheduleOccurrenceRequest>("application/json")
            .Produces<ScheduleOccurrenceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapPatch("/api/events/{eventId}/occurrences/{occurrenceId}", RescheduleOccurrence)
            .WithName("RescheduleOccurrence")
            .WithTags("Occurrences")
            .RequireAuthorization()
            .RequireCompleteJsonBody<RescheduleOccurrenceRequest>()
            .Accepts<RescheduleOccurrenceRequest>("application/json")
            .Produces<RescheduleOccurrenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapDelete("/api/events/{eventId}/occurrences/{occurrenceId}", CancelOccurrence)
            .WithName("CancelOccurrence")
            .WithTags("Occurrences")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/events/{eventId}/occurrences", GetEventOccurrences)
            .WithName("GetEventOccurrences")
            .WithTags("Occurrences")
            .Produces<IReadOnlyList<OccurrenceResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ScheduleOccurrence(
        int eventId,
        ScheduleOccurrenceRequest request,
        ISender sender)
    {
        var command = new ScheduleOccurrenceCommand(
            eventId,
            request.StartsAt,
            request.EndsAt,
            request.VenueName,
            request.Address);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var occurrence = result.Value!;

        return Results.Json(
            new ScheduleOccurrenceResponse(
                occurrence.OccurrenceId,
                occurrence.StartsAt,
                occurrence.EndsAt,
                occurrence.VenueName,
                occurrence.Address,
                occurrence.CreatedAt),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> RescheduleOccurrence(
        int eventId,
        int occurrenceId,
        RescheduleOccurrenceRequest request,
        ISender sender)
    {
        var command = new RescheduleOccurrenceCommand(
            eventId,
            occurrenceId,
            request.StartsAt,
            request.EndsAt,
            request.VenueName,
            request.Address);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        var occurrence = result.Value!;

        return Results.Ok(new RescheduleOccurrenceResponse(
            occurrence.OccurrenceId,
            occurrence.StartsAt,
            occurrence.EndsAt,
            occurrence.VenueName,
            occurrence.Address,
            occurrence.UpdatedAt));
    }

    private static async Task<IResult> CancelOccurrence(
        int eventId,
        int occurrenceId,
        ISender sender)
    {
        var command = new CancelOccurrenceCommand(eventId, occurrenceId);

        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            return result.ToHttpResult();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetEventOccurrences(
        int eventId,
        ISender sender)
    {
        var query = new GetEventOccurrencesQuery(eventId);

        var result = await sender.Send(query);

        return result.ToHttpResult();
    }
}
