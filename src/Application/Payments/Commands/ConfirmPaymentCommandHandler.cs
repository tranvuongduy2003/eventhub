using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Payments;

namespace EventHub.Application.Payments.Commands;

public sealed class ConfirmPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<ConfirmPaymentCommand, PaymentNotificationResult>
{
    public override async Task<Result<PaymentNotificationResult>> Handle(
        ConfirmPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByProviderReferenceAsync(
            command.ProviderReference,
            cancellationToken);

        if (payment is null)
        {
            return PaymentErrors.PaymentNotFound;
        }

        var order = await orderRepository.GetByIdAsync(payment.OrderId, cancellationToken);
        if (order is null)
        {
            return PaymentErrors.OrderNotFound;
        }

        if (payment.Amount.Amount != order.Total.Amount ||
            payment.Amount.Currency != order.Total.Currency)
        {
            return PaymentErrors.PaymentAmountMismatch;
        }

        if (payment.Status is PaymentStatus.Captured)
        {
            return new PaymentNotificationResult(
                payment.Id.Value,
                order.Id.Value,
                payment.Status.ToString().ToLowerInvariant(),
                order.Status.ToString().ToLowerInvariant(),
                Applied: false);
        }

        try
        {
            var now = clock.UtcNow;
            var applied = payment.Capture(now);
            order.MarkConfirmed(payment.Id.Value, now);

            if (order.ReservationId is not null)
            {
                var eventAggregate = await eventRepository.GetByIdAsync(order.EventId, cancellationToken);
                if (eventAggregate is not null)
                {
                    eventAggregate.CommitReservation(order.ReservationId.Value, now);
                    order.ClearReservationId();
                    await eventRepository.Update(eventAggregate, cancellationToken);

                    pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
                    eventAggregate.ClearDomainEvents();
                }
            }

            await paymentRepository.Update(payment, cancellationToken);
            await orderRepository.Update(order, cancellationToken);

            pendingDomainEventsCollector.AddRange(payment.DomainEvents);
            payment.ClearDomainEvents();
            pendingDomainEventsCollector.AddRange(order.DomainEvents);
            order.ClearDomainEvents();

            return new PaymentNotificationResult(
                payment.Id.Value,
                order.Id.Value,
                payment.Status.ToString().ToLowerInvariant(),
                order.Status.ToString().ToLowerInvariant(),
                applied);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
