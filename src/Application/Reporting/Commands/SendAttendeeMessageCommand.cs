using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Commands;

public sealed record SendAttendeeMessageCommand(int EventId, string Subject, string Body)
    : ICommand<SendAttendeeMessageResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.Reporting;
}

public sealed record SendAttendeeMessageResult(int AcceptedRecipientCount);
