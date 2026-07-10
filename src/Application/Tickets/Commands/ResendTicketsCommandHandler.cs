using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Orders;

namespace EventHub.Application.Tickets.Commands;

public sealed class ResendTicketsCommandHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    IEmailSender emailSender,
    IClock clock)
    : CommandHandler<ResendTicketsCommand, ResendTicketsResult>
{
    public override async Task<Result<ResendTicketsResult>> Handle(
        ResendTicketsCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var order = await orderRepository.GetByIdAsync(OrderId.From(command.OrderId), cancellationToken);

        if (order is null || order.Contact.Email != normalizedEmail)
        {
            return new ResendTicketsResult(true);
        }

        var tickets = await ticketRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (tickets.Count == 0)
        {
            return new ResendTicketsResult(true);
        }

        var eventAggregate = await eventRepository.GetByIdAsync(order.EventId, cancellationToken);
        var results = TicketProjection.ToResults(tickets, eventAggregate);
        await emailSender.SendAsync(TicketEmailComposer.Create(normalizedEmail, results), cancellationToken);

        var deliveredAt = clock.UtcNow;
        foreach (var ticket in tickets)
        {
            ticket.MarkDelivered(deliveredAt);
        }

        await ticketRepository.UpdateRangeAsync(tickets, cancellationToken);

        return new ResendTicketsResult(true);
    }
}
