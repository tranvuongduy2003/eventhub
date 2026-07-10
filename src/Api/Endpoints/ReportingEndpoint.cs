using EventHub.Api.Http;
using EventHub.Api.Mapping;
using EventHub.Application.Reporting;
using EventHub.Application.Reporting.Commands;
using EventHub.Application.Reporting.Queries;
using EventHub.Contracts.Reporting;
using MediatR;

namespace EventHub.Api.Endpoints;

internal sealed class ReportingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/events/{eventId:int}/audience/attendees", ListAttendees)
            .WithName("ListEventAttendees")
            .WithTags("Reporting")
            .RequireAuthorization()
            .Produces<EventAttendeeListResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        endpoints.MapGet("/api/events/{eventId:int}/audience/attendees.csv", ExportAttendees)
            .WithName("ExportEventAttendees")
            .WithTags("Reporting")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        endpoints.MapGet("/api/events/{eventId:int}/results", GetResults)
            .WithName("GetEventResults")
            .WithTags("Reporting")
            .RequireAuthorization()
            .Produces<EventResultsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        endpoints.MapGet("/api/organizer/audience/events", GetOrganizerOverview)
            .WithName("GetOrganizerAudienceOverview")
            .WithTags("Reporting")
            .RequireAuthorization()
            .Produces<OrganizerAudienceOverviewResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapPost("/api/events/{eventId:int}/audience/messages", SendMessage)
            .WithName("SendAttendeeMessage")
            .WithTags("Reporting")
            .RequireAuthorization()
            .RequireCompleteJsonBody<SendAttendeeMessageRequest>()
            .Accepts<SendAttendeeMessageRequest>("application/json")
            .Produces<SendAttendeeMessageResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        endpoints.MapPut("/api/events/{eventId:int}/audience/reminder", SetReminder)
            .WithName("SetEventReminder")
            .WithTags("Reporting")
            .RequireAuthorization()
            .RequireCompleteJsonBody<EventReminderSettingsRequest>()
            .Accepts<EventReminderSettingsRequest>("application/json")
            .Produces<EventReminderSettingsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> ListAttendees(int eventId, ISender sender)
    {
        var result = await sender.Send(new ListEventAttendeesQuery(eventId));

        return result.ToHttpResult(attendees => Results.Ok(new EventAttendeeListResponse(
            attendees.Select(ToResponse).ToList())));
    }

    private static async Task<IResult> ExportAttendees(int eventId, ISender sender)
    {
        var result = await sender.Send(new ExportEventAttendeesCsvQuery(eventId));

        return result.ToHttpResult(csv => Results.Text(
            csv,
            contentType: "text/csv",
            statusCode: StatusCodes.Status200OK));
    }

    private static async Task<IResult> GetResults(int eventId, ISender sender)
    {
        var result = await sender.Send(new GetEventResultsQuery(eventId));

        return result.ToHttpResult(results => Results.Ok(ToResponse(results)));
    }

    private static async Task<IResult> GetOrganizerOverview(ISender sender)
    {
        var result = await sender.Send(new GetOrganizerAudienceOverviewQuery());

        return result.ToHttpResult(overview => Results.Ok(new OrganizerAudienceOverviewResponse(
            overview.OwnedEvents.Select(ToResponse).ToList(),
            overview.StaffEvents.Select(ToResponse).ToList())));
    }

    private static async Task<IResult> SendMessage(
        int eventId,
        SendAttendeeMessageRequest request,
        ISender sender)
    {
        var result = await sender.Send(new SendAttendeeMessageCommand(eventId, request.Subject, request.Body));

        return result.ToHttpResult(message => Results.Json(
            new SendAttendeeMessageResponse(message.AcceptedRecipientCount),
            statusCode: StatusCodes.Status202Accepted));
    }

    private static async Task<IResult> SetReminder(
        int eventId,
        EventReminderSettingsRequest request,
        ISender sender)
    {
        var result = await sender.Send(new SetEventReminderCommand(
            eventId,
            request.Enabled,
            request.LeadTimeMinutes));

        return result.ToHttpResult(reminder => Results.Ok(ToResponse(reminder)));
    }

    private static EventAttendeeResponse ToResponse(EventAttendeeResult attendee) =>
        new(
            attendee.Name,
            attendee.Email,
            attendee.TicketTypeId,
            attendee.TicketTypeName,
            attendee.OrderId,
            attendee.TicketId,
            attendee.CheckedIn,
            attendee.CheckedInAt);

    private static EventResultsResponse ToResponse(EventResultsResult results) =>
        new(
            results.EventId,
            results.EventTitle,
            results.TotalRevenueAmount,
            results.TotalRevenueCurrency,
            results.IssuedCount,
            results.CheckedInCount,
            results.NoShowCount,
            results.CheckInRate,
            results.TicketsSoldByType.Select(ToResponse).ToList());

    private static TicketTypeSalesResponse ToResponse(TicketTypeSalesResult sales) =>
        new(
            sales.TicketTypeId,
            sales.TicketTypeName,
            sales.SoldCount,
            sales.RevenueAmount,
            sales.RevenueCurrency);

    private static OwnedEventOverviewResponse ToResponse(OwnedEventOverviewResult item) =>
        new(
            item.EventId,
            item.Title,
            item.Status,
            item.StartsAt,
            item.TimeZoneId,
            item.SoldCount,
            item.TotalRevenueAmount,
            item.TotalRevenueCurrency,
            item.CheckedInCount,
            item.IssuedCount);

    private static StaffEventOverviewResponse ToResponse(StaffEventOverviewResult item) =>
        new(
            item.EventId,
            item.Title,
            item.Status,
            item.StartsAt,
            item.TimeZoneId,
            item.CheckedInCount,
            item.IssuedCount);

    private static EventReminderSettingsResponse ToResponse(EventReminderSettingsResult reminder) =>
        new(
            reminder.EventId,
            reminder.Enabled,
            reminder.LeadTimeMinutes,
            reminder.UpdatedAt,
            reminder.LastSentAt);
}
