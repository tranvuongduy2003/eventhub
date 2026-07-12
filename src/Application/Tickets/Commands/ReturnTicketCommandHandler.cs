using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Payments;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets.Commands;

public sealed class ReturnTicketCommandHandler(
    ITicketRepository ticketRepository,
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IEventRepository eventRepository,
    IPaymentGateway paymentGateway,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<ReturnTicketCommand, ReturnTicketResult>
{
    public override async Task<Result<ReturnTicketResult>> Handle(
        ReturnTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(TicketId.From(command.TicketId), cancellationToken);
        if (ticket is null || ticket.OrderId.Value != command.OrderId)
        {
            return ReturnTicketErrors.TicketNotFound;
        }

        var order = await orderRepository.GetByIdAsync(OrderId.From(command.OrderId), cancellationToken);
        if (order is null)
        {
            return ReturnTicketErrors.OrderNotFound;
        }

        var eventAggregate = await eventRepository.GetByIdAsync(ticket.EventId, cancellationToken);
        if (eventAggregate is null)
        {
            return ReturnTicketErrors.EventNotFound;
        }

        if (eventAggregate.Schedule is not null && clock.UtcNow >= eventAggregate.Schedule.StartsAt)
        {
            return ReturnTicketErrors.ReturnCutoffPassed;
        }

        var ticketType = eventAggregate.TicketTypes.SingleOrDefault(type => type.Id == ticket.TicketTypeId);
        if (ticketType is null || ticketType.Available > 0)
        {
            return ReturnTicketErrors.EventNotSoldOut;
        }

        try
        {
            var now = clock.UtcNow;
            ticket.Return(now);
            eventAggregate.ReturnToPool(ticket.TicketTypeId, quantity: 1, now);

            Payment? payment = null;
            if (order.Total.Amount > 0)
            {
                payment = await paymentRepository.GetLatestByOrderIdAsync(order.Id, cancellationToken);
                if (payment is { Status: PaymentStatus.Captured })
                {
                    await paymentGateway.RefundPaymentAsync(
                        new PaymentRefundRequest(
                            payment.Id.Value,
                            order.Id.Value,
                            order.Total.Amount,
                            order.Total.Currency,
                            payment.ProviderReference.Value),
                        cancellationToken);

                    payment.Refund(now);
                    await paymentRepository.Update(payment, cancellationToken);
                    pendingDomainEventsCollector.AddRange(payment.DomainEvents);
                    payment.ClearDomainEvents();
                }
            }

            order.MarkRefunded(now);

            await ticketRepository.UpdateAsync(ticket, cancellationToken);
            await orderRepository.Update(order, cancellationToken);
            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(ticket.DomainEvents);
            ticket.ClearDomainEvents();
            pendingDomainEventsCollector.AddRange(order.DomainEvents);
            order.ClearDomainEvents();
            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new ReturnTicketResult(
                ticket.Id.Value,
                order.Id.Value,
                eventAggregate.Id.Value,
                ticket.Status.ToString().ToLowerInvariant(),
                order.Status.ToString().ToLowerInvariant(),
                payment?.Status.ToString().ToLowerInvariant());
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
