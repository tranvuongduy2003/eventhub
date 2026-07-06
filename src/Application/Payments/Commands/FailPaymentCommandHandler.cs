using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Payments.Commands;

public sealed class FailPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IOrderRepository orderRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<FailPaymentCommand, PaymentNotificationResult>
{
    public override async Task<Result<PaymentNotificationResult>> Handle(
        FailPaymentCommand command,
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

        try
        {
            var applied = payment.Fail(clock.UtcNow);

            await paymentRepository.Update(payment, cancellationToken);

            pendingDomainEventsCollector.AddRange(payment.DomainEvents);
            payment.ClearDomainEvents();

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
