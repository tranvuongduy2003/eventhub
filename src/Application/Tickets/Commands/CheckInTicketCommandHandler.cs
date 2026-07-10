using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets.Commands;

public sealed class CheckInTicketByCodeCommandHandler(
    ITicketRepository ticketRepository,
    IOrderRepository orderRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<CheckInTicketByCodeCommand, CheckInTicketResult>
{
    public override async Task<Result<CheckInTicketResult>> Handle(
        CheckInTicketByCodeCommand command,
        CancellationToken cancellationToken)
    {
        TicketCode code;
        try
        {
            code = TicketCode.Create(command.Code?.Trim() ?? string.Empty);
        }
        catch (BusinessRuleValidationException)
        {
            return CheckInErrors.UnknownTicket;
        }

        var ticket = await ticketRepository.GetByCodeAsync(code, cancellationToken);
        if (ticket is null)
        {
            return CheckInErrors.UnknownTicket;
        }

        return await CheckInTicketCommandHandlerCore.CheckInAsync(
            ticket,
            EventId.From(command.EventId),
            ticketRepository,
            orderRepository,
            clock,
            pendingDomainEventsCollector,
            cancellationToken);
    }
}

public sealed class CheckInTicketByIdCommandHandler(
    ITicketRepository ticketRepository,
    IOrderRepository orderRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<CheckInTicketByIdCommand, CheckInTicketResult>
{
    public override async Task<Result<CheckInTicketResult>> Handle(
        CheckInTicketByIdCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);
        var ticket = await ticketRepository.GetByIdForEventAsync(
            TicketId.From(command.TicketId),
            eventId,
            cancellationToken);

        if (ticket is null)
        {
            return CheckInErrors.TicketNotFound;
        }

        return await CheckInTicketCommandHandlerCore.CheckInAsync(
            ticket,
            eventId,
            ticketRepository,
            orderRepository,
            clock,
            pendingDomainEventsCollector,
            cancellationToken);
    }
}

file static class CheckInTicketCommandHandlerCore
{
    public static async Task<Result<CheckInTicketResult>> CheckInAsync(
        Ticket ticket,
        EventId eventId,
        ITicketRepository ticketRepository,
        IOrderRepository orderRepository,
        IClock clock,
        IPendingDomainEventsCollector pendingDomainEventsCollector,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(ticket.OrderId, cancellationToken);
        if (order?.Status != OrderStatus.Confirmed)
        {
            return CheckInErrors.TicketOrderNotConfirmed;
        }

        try
        {
            ticket.CheckIn(eventId, clock.UtcNow);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }

        await ticketRepository.UpdateAsync(ticket, cancellationToken);
        pendingDomainEventsCollector.AddRange(ticket.DomainEvents);
        ticket.ClearDomainEvents();

        return CheckInTicketProjection.ToResult(ticket);
    }
}
