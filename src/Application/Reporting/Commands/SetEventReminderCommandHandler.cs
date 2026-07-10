using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Commands;

public sealed class SetEventReminderCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPermissionCache permissionCache,
    IEventRepository eventRepository,
    IReportingRepository reportingRepository,
    IClock clock)
    : CommandHandler<SetEventReminderCommand, EventReminderSettingsResult>
{
    public override async Task<Result<EventReminderSettingsResult>> Handle(
        SetEventReminderCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);
        if (!await IsOwnerAsync(eventId, cancellationToken))
        {
            return ReportingErrors.InsufficientPermissions;
        }

        if (command.LeadTimeMinutes <= 0)
        {
            return ReportingErrors.InvalidReminderLeadTime;
        }

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (command.Enabled
            && eventAggregate?.Schedule?.StartsAt is { } startsAt
            && startsAt.AddMinutes(-command.LeadTimeMinutes) <= clock.UtcNow)
        {
            return ReportingErrors.ReminderWindowAlreadyPassed;
        }

        return await reportingRepository.SetReminderSettingsAsync(
            eventId,
            command.Enabled,
            command.LeadTimeMinutes,
            clock.UtcNow,
            cancellationToken);
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
