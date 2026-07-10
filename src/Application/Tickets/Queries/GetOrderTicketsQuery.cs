using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Tickets.Queries;

public sealed record GetOrderTicketsQuery(int OrderId) : IQuery<GetOrderTicketsResult>;

public sealed record GetOrderTicketsResult(int OrderId, string OrderStatus, List<TicketResult> Tickets);
