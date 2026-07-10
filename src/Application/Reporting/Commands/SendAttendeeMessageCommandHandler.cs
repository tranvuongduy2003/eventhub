using System.Net;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Commands;

public sealed class SendAttendeeMessageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPermissionCache permissionCache,
    IEventRepository eventRepository,
    IReportingRepository reportingRepository,
    IEmailSender emailSender)
    : CommandHandler<SendAttendeeMessageCommand, SendAttendeeMessageResult>
{
    public override async Task<Result<SendAttendeeMessageResult>> Handle(
        SendAttendeeMessageCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);
        if (!await IsOwnerAsync(eventId, cancellationToken))
        {
            return ReportingErrors.InsufficientPermissions;
        }

        var subject = command.Subject.Trim();
        var body = command.Body.Trim();

        if (string.IsNullOrWhiteSpace(subject))
        {
            return ReportingErrors.MessageSubjectRequired;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return ReportingErrors.MessageBodyRequired;
        }

        var attendees = await reportingRepository.ListAttendeesAsync(eventId, cancellationToken);
        if (attendees.Count == 0)
        {
            return ReportingErrors.AttendeesRequired;
        }

        foreach (var attendee in attendees)
        {
            await emailSender.SendAsync(
                new EmailMessage(
                    attendee.Email,
                    subject,
                    $"<p>{WebUtility.HtmlEncode(body)}</p>"),
                cancellationToken);
        }

        return new SendAttendeeMessageResult(attendees.Count);
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
}
