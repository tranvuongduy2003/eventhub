using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Tickets;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets.Commands;

public sealed class TransferTicketCommandHandler(
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    ITicketCodeGenerator ticketCodeGenerator,
    IEmailSender emailSender,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<TransferTicketCommand, TicketResult>
{
    public override async Task<Result<TicketResult>> Handle(
        TransferTicketCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(command.OrderId);
        var ticket = await ticketRepository.GetByIdAsync(TicketId.From(command.TicketId), cancellationToken);
        if (ticket is null || ticket.OrderId != orderId)
        {
            return TransferTicketErrors.TicketNotFound;
        }

        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order?.Status != OrderStatus.Confirmed)
        {
            return TransferTicketErrors.TicketOrderNotConfirmed;
        }

        Ticket replacementTicket;
        try
        {
            var recipient = Contact.Create(command.RecipientName, command.RecipientEmail);
            var replacementCode = await GenerateUniqueCodeAsync(cancellationToken);
            replacementTicket = ticket.Transfer(recipient, replacementCode, clock.UtcNow);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }

        pendingDomainEventsCollector.AddRange(ticket.DomainEvents);
        pendingDomainEventsCollector.AddRange(replacementTicket.DomainEvents);
        ticket.ClearDomainEvents();
        replacementTicket.ClearDomainEvents();

        var eventAggregate = await eventRepository.GetByIdAsync(replacementTicket.EventId, cancellationToken);
        var results = TicketProjection.ToResults([replacementTicket], eventAggregate);
        await emailSender.SendAsync(TicketEmailComposer.Create(replacementTicket.Holder.Email, results), cancellationToken);

        replacementTicket.MarkDelivered(clock.UtcNow);
        await ticketRepository.UpdateAsync(ticket, cancellationToken);
        await ticketRepository.AddRangeAsync([replacementTicket], cancellationToken);

        return results[0];
    }

    private async Task<TicketCode> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var code = TicketCode.Create(ticketCodeGenerator.Generate());
            if (!await ticketRepository.ExistsByCodeAsync(code, cancellationToken))
            {
                return code;
            }
        }
    }
}
