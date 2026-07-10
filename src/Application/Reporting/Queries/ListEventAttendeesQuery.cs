using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Queries;

public sealed record ListEventAttendeesQuery(int EventId)
    : IQuery<IReadOnlyList<EventAttendeeResult>>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.Reporting;
}
