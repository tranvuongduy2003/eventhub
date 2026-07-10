using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Tickets;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets.EventHandlers;

internal sealed class IssueTicketsOnOrderConfirmedHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    ITicketCodeGenerator ticketCodeGenerator,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock)
    : IDomainEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent domainEvent, CancellationToken cancellationToken)
    {
        var existingTickets = await ticketRepository.GetByOrderIdAsync(domainEvent.OrderId, cancellationToken);
        if (existingTickets.Count > 0)
        {
            await DeliverAsync(existingTickets, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null || order.Status is not OrderStatus.Confirmed)
        {
            return;
        }

        var issuedAt = clock.UtcNow;
        var tickets = new List<Ticket>();

        foreach (var line in order.Lines)
        {
            for (var count = 0; count < line.Quantity; count++)
            {
                tickets.Add(Ticket.Issue(
                    order.EventId,
                    order.Id,
                    line.TicketTypeId,
                    order.Contact,
                    await GenerateUniqueCodeAsync(cancellationToken),
                    issuedAt));
            }
        }

        await DeliverNewAsync(tickets, cancellationToken);
        await ticketRepository.AddRangeAsync(tickets, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TicketCode> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = TicketCode.Create(ticketCodeGenerator.Generate());
            if (!await ticketRepository.ExistsByCodeAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not generate a unique ticket code.");
    }

    private async Task DeliverAsync(List<Ticket> tickets, CancellationToken cancellationToken)
    {
        var eventAggregate = await eventRepository.GetByIdAsync(tickets[0].EventId, cancellationToken);
        var results = TicketProjection.ToResults(tickets, eventAggregate);
        await emailSender.SendAsync(TicketEmailComposer.Create(tickets[0].Holder.Email, results), cancellationToken);

        var deliveredAt = clock.UtcNow;
        foreach (var ticket in tickets)
        {
            ticket.MarkDelivered(deliveredAt);
        }

        await ticketRepository.UpdateRangeAsync(tickets, cancellationToken);
    }

    private async Task DeliverNewAsync(List<Ticket> tickets, CancellationToken cancellationToken)
    {
        var eventAggregate = await eventRepository.GetByIdAsync(tickets[0].EventId, cancellationToken);
        var results = TicketProjection.ToResults(tickets, eventAggregate);
        await emailSender.SendAsync(TicketEmailComposer.Create(tickets[0].Holder.Email, results), cancellationToken);

        var deliveredAt = clock.UtcNow;
        foreach (var ticket in tickets)
        {
            ticket.MarkDelivered(deliveredAt);
        }
    }
}
