using System.Text;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Queries;

public sealed class ExportEventAttendeesCsvQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPermissionCache permissionCache,
    IEventRepository eventRepository,
    IReportingRepository reportingRepository)
    : QueryHandler<ExportEventAttendeesCsvQuery, string>
{
    public override async Task<Result<string>> Handle(
        ExportEventAttendeesCsvQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(query.EventId);
        if (!await IsOwnerAsync(eventId, cancellationToken))
        {
            return ReportingErrors.InsufficientPermissions;
        }

        var attendees = await reportingRepository.ListAttendeesAsync(eventId, cancellationToken);
        var csv = new StringBuilder("name,email,ticketTypeName,orderId,ticketId,checkedIn,checkedInAt\r\n");

        foreach (var attendee in attendees)
        {
            csv.AppendJoin(
                ',',
                Escape(attendee.Name),
                Escape(attendee.Email),
                Escape(attendee.TicketTypeName),
                attendee.OrderId,
                attendee.TicketId,
                attendee.CheckedIn,
                attendee.CheckedInAt?.ToString("O") ?? string.Empty);
            csv.Append("\r\n");
        }

        return csv.ToString();
    }

    private async Task<bool> IsOwnerAsync(EventId eventId, CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return false;
        }

        var role = await permissionCache.GetRoleAsync(eventId, userId, cancellationToken);
        if (role is EventRole.Owner)
        {
            return true;
        }

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        return eventAggregate?.OrganizerId == userId;
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
