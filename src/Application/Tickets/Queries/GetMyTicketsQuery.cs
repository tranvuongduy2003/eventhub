using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Tickets.Queries;

public sealed record GetMyTicketsQuery : IQuery<List<TicketResult>>;
