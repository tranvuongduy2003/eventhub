using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Payments;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;

namespace EventHub.Application.Events.Commands;

public sealed class CancelEventCommandHandler(
    IEventRepository eventRepository,
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    ITicketRepository ticketRepository,
    IPaymentGateway paymentGateway,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<CancelEventCommand, CancelEventResult>
{
    public override async Task<Result<CancelEventResult>> Handle(
        CancelEventCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return EventCancelErrors.EventNotFound;
        }

        try
        {
            var now = clock.UtcNow;
            eventAggregate.Cancel(now);

            var confirmedOrders = await orderRepository.GetConfirmedByEventIdAsync(eventId, cancellationToken);
            var capturedPayments = await paymentRepository.GetCapturedByOrderIdsAsync(
                confirmedOrders.Select(order => order.Id).ToList(),
                cancellationToken);
            var latestCapturedPaymentByOrderId = capturedPayments
                .GroupBy(payment => payment.OrderId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(payment => payment.CapturedAt).First());

            var tickets = await ticketRepository.GetByOrderIdsAsync(
                confirmedOrders.Select(order => order.Id).ToList(),
                cancellationToken);

            foreach (var ticket in tickets)
            {
                ticket.Void(now);
            }

            foreach (var order in confirmedOrders)
            {
                if (latestCapturedPaymentByOrderId.TryGetValue(order.Id, out var payment))
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

                    order.MarkRefunded(now);
                }
                else
                {
                    order.Cancel(now);
                }

                await orderRepository.Update(order, cancellationToken);
                pendingDomainEventsCollector.AddRange(order.DomainEvents);
                order.ClearDomainEvents();
            }

            await eventRepository.Update(eventAggregate, cancellationToken);
            await ticketRepository.UpdateRangeAsync(tickets, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();
            foreach (var ticket in tickets)
            {
                pendingDomainEventsCollector.AddRange(ticket.DomainEvents);
                ticket.ClearDomainEvents();
            }

            return new CancelEventResult(
                eventAggregate.Status.ToString(),
                eventAggregate.CancelledAt!.Value,
                eventAggregate.UpdatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
