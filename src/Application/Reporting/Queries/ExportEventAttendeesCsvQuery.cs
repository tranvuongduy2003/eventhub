using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Queries;

public sealed record ExportEventAttendeesCsvQuery(int EventId)
    : IQuery<string>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.Reporting;
}
