using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Tickets.Queries;

public sealed record SearchCheckInTicketsQuery(int EventId, string Query)
    : IQuery<IReadOnlyList<CheckInTicketResult>>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.CheckIn;
}
