using EventHub.Application.Abstractions.Payments;

namespace EventHub.Infrastructure.Payments;

public sealed class LocalPaymentGateway : IPaymentGateway
{
    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var providerReference =
            request.ExistingProviderReference ?? $"local-payment-{request.OrderId}-{Guid.NewGuid():N}";

        return Task.FromResult(new PaymentInitiationResult(
            RedirectUrl: request.SuccessUrl,
            ProviderReference: providerReference));
    }

    public Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new PaymentRefundResult(
            request.ProviderReference,
            Applied: true));
    }
}
