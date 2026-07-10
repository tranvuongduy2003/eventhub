using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Commands;

public sealed class ProcessDueEventRemindersCommandHandler(
    IReportingRepository reportingRepository,
    IEmailSender emailSender,
    IClock clock)
    : CommandHandler<ProcessDueEventRemindersCommand, ProcessDueEventRemindersResult>
{
    public override async Task<Result<ProcessDueEventRemindersResult>> Handle(
        ProcessDueEventRemindersCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var dueReminders = await reportingRepository.GetDueRemindersAsync(now, cancellationToken);
        var recipientCount = 0;

        foreach (var reminder in dueReminders)
        {
            var attendees = await reportingRepository.ListAttendeesAsync(
                EventId.From(reminder.EventId),
                cancellationToken);

            foreach (var attendee in attendees)
            {
                await emailSender.SendAsync(
                    new EmailMessage(
                        attendee.Email,
                        $"Reminder: {reminder.EventTitle}",
                        $"<p>{reminder.EventTitle} starts at {reminder.StartsAt:u} ({reminder.TimeZoneId ?? "UTC"}).</p>"),
                    cancellationToken);
                recipientCount++;
            }

            await reportingRepository.MarkReminderSentAsync(
                EventId.From(reminder.EventId),
                now,
                cancellationToken);
        }

        return new ProcessDueEventRemindersResult(dueReminders.Count, recipientCount);
    }
}
