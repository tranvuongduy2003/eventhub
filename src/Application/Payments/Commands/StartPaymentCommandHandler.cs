using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Payments;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;

namespace EventHub.Application.Payments.Commands;

public sealed class StartPaymentCommandHandler(
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IPaymentIdGenerator paymentIdGenerator,
    IPaymentGateway paymentGateway,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<StartPaymentCommand, StartPaymentResult>
{
    public override async Task<Result<StartPaymentResult>> Handle(
        StartPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(OrderId.From(command.OrderId), cancellationToken);
        if (order is null)
        {
            return PaymentErrors.OrderNotFound;
        }

        if (order.Total.Amount == 0)
        {
            return PaymentErrors.PaymentNotRequired;
        }

        if (order.Status is not OrderStatus.Pending)
        {
            return PaymentErrors.OrderNotPayable;
        }

        var existingPayment = await paymentRepository.GetLatestByOrderIdAsync(order.Id, cancellationToken);
        if (existingPayment is { Status: PaymentStatus.Initiated })
        {
            var existingGatewayResult = await paymentGateway.InitiatePaymentAsync(
                new PaymentInitiationRequest(
                    order.Id.Value,
                    order.Total.Amount,
                    order.Total.Currency,
                    command.SuccessUrl,
                    command.CancelUrl,
                    existingPayment.ProviderReference.Value),
                cancellationToken);

            return new StartPaymentResult(
                existingPayment.Id.Value,
                order.Id.Value,
                existingPayment.Amount.Amount,
                existingPayment.Amount.Currency,
                existingPayment.ProviderReference.Value,
                existingGatewayResult.RedirectUrl);
        }

        var gatewayResult = await paymentGateway.InitiatePaymentAsync(
            new PaymentInitiationRequest(
                order.Id.Value,
                order.Total.Amount,
                order.Total.Currency,
                command.SuccessUrl,
                command.CancelUrl,
                ExistingProviderReference: null),
            cancellationToken);

        try
        {
            var paymentId = await paymentIdGenerator.NextIdAsync(cancellationToken);
            var payment = Payment.Initiate(
                order.Id,
                order.Total,
                ProviderReference.Create(gatewayResult.ProviderReference),
                clock.UtcNow,
                paymentId);

            await paymentRepository.AddAsync(payment, cancellationToken);

            pendingDomainEventsCollector.AddRange(payment.DomainEvents);
            payment.ClearDomainEvents();

            return new StartPaymentResult(
                payment.Id.Value,
                order.Id.Value,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.ProviderReference.Value,
                gatewayResult.RedirectUrl);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
