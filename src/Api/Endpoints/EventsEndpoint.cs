using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Events.Commands;
using EventHub.Application.Events.Queries;
using EventHub.Contracts.Events;
using MediatR;
using DatePreset = EventHub.Application.Common.DatePreset;
using EventFilter = EventHub.Application.Common.EventFilter;

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

        endpoints.MapGet("/api/events/{eventId:int}", GetEventDetails)
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

        endpoints.MapGet("/api/events", ListPublicEvents)
            .WithName("ListPublicEvents")
            .WithTags("Events")
            .AllowAnonymous()
            .Produces<PublicEventListingResponse>(StatusCodes.Status200OK);

        endpoints.MapGet("/api/events/locations", ListEventLocations)
            .WithName("ListEventLocations")
            .WithTags("Events")
            .AllowAnonymous()
            .Produces<List<string>>(StatusCodes.Status200OK);

        endpoints.MapGet("/api/events/{slug}", GetPublicEvent)
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
        string slug,
        ISender sender)
    {
        var query = new GetPublicEventQuery(slug);

        var result = await sender.Send(query);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListPublicEvents(
        int? page,
        int? pageSize,
        string? q,
        string? date,
        string? dateFrom,
        string? dateTo,
        string? location,
        IClock clock,
        ISender sender)
    {
        DatePreset? datePreset = date?.ToLowerInvariant() switch
        {
            "today" => DatePreset.Today,
            "tomorrow" => DatePreset.Tomorrow,
            "this-week" => DatePreset.ThisWeek,
            "this-month" => DatePreset.ThisMonth,
            _ => null,
        };

        DateTimeOffset? parsedDateFrom = null;
        DateTimeOffset? parsedDateTo = null;

        if (DateOnly.TryParse(dateFrom, out var df))
        {
            parsedDateFrom = new DateTimeOffset(df.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        if (DateOnly.TryParse(dateTo, out var dt))
        {
            parsedDateTo = new DateTimeOffset(dt.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        }

        // Resolve date preset to concrete date range at the API layer
        if (datePreset.HasValue)
        {
            var (presetFrom, presetTo) = ComputeDateRangeFromPreset(datePreset.Value, clock.UtcNow);
            parsedDateFrom ??= presetFrom;
            parsedDateTo ??= presetTo;
        }

        var filter = new EventFilter(
            Search: q,
            DateFrom: parsedDateFrom,
            DateTo: parsedDateTo,
            Location: location);

        var query = new ListPublicEventsQuery(
            page is > 0 ? page.Value : 1,
            pageSize is > 0 ? pageSize.Value : 24,
            filter);

        var result = await sender.Send(query);

        return result.ToHttpResult();
    }

    private static (DateTimeOffset From, DateTimeOffset To) ComputeDateRangeFromPreset(
        DatePreset preset,
        DateTimeOffset now)
    {
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        return preset switch
        {
            DatePreset.Today => (today, today.AddDays(1).AddTicks(-1)),
            DatePreset.Tomorrow => (today.AddDays(1), today.AddDays(2).AddTicks(-1)),
            DatePreset.ThisWeek =>
                // ISO 8601: Monday = start of week (inclusive) to Sunday end of day
                // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
                // Shift so Monday=0: (dayOfWeek + 6) % 7
                (today.AddDays(-(((int)today.DayOfWeek + 6) % 7)),
                 today.AddDays(-(((int)today.DayOfWeek + 6) % 7) + 7).AddTicks(-1)),
            DatePreset.ThisMonth => (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero),
                                     new DateTimeOffset(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, TimeSpan.Zero).AddTicks(9999999)),
            _ => (today, today.AddDays(1).AddTicks(-1)),
        };
    }

    private static async Task<IResult> ListEventLocations(
        ISender sender)
    {
        var query = new ListEventLocationsQuery();

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
