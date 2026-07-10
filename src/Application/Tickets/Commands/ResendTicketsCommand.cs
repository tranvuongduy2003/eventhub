using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Tickets.Commands;

public sealed record ResendTicketsCommand(int OrderId, string Email) : ICommand<ResendTicketsResult>;

public sealed record ResendTicketsResult(bool Accepted);
