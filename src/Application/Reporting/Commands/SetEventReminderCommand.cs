using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Commands;

public sealed record SetEventReminderCommand(int EventId, bool Enabled, int LeadTimeMinutes)
    : ICommand<EventReminderSettingsResult>, IAuthorizeEventOperation, IUnitOfWorkRequest
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.Reporting;
}
