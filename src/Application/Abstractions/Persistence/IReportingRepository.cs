using EventHub.Application.Reporting;
using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IReportingRepository
{
    Task<IReadOnlyList<EventAttendeeResult>> ListAttendeesAsync(
        EventId eventId,
        CancellationToken cancellationToken = default);

    Task<EventResultsResult> GetEventResultsAsync(
        EventId eventId,
        CancellationToken cancellationToken = default);

    Task<OrganizerAudienceOverviewResult> GetOrganizerOverviewAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<EventReminderSettingsResult> SetReminderSettingsAsync(
        EventId eventId,
        bool enabled,
        int leadTimeMinutes,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DueEventReminderResult>> GetDueRemindersAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task MarkReminderSentAsync(
        EventId eventId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default);
}
