using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Tickets.Commands;

public sealed record BatchCheckInTicketsCommand(
    int EventId,
    IReadOnlyList<BatchCheckInTicketRequest> Tickets)
    : ICommand<BatchCheckInTicketsResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.CheckIn;
}

public sealed record BatchCheckInTicketRequest(string ClientScanId, string Code, DateTimeOffset ScannedAt);

public sealed record BatchCheckInTicketsResult(IReadOnlyList<BatchCheckInTicketResult> Results);

public sealed record BatchCheckInTicketResult(
    string ClientScanId,
    string Code,
    bool Accepted,
    string Status,
    string? Reason,
    CheckInTicketResult? Ticket);
