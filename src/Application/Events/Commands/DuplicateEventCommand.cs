using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record DuplicateEventCommand(int EventId)
    : ICommand<DuplicateEventResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.EventManagement;
}

public sealed record DuplicateEventResult(string Status, DateTimeOffset CreatedAt);
