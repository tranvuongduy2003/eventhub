using EventHub.Application.Abstractions.Payments;

namespace EventHub.Infrastructure.Payments;

public sealed class NoOpPaymentGateway : IPaymentGateway
{
    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentInitiationResult(
            RedirectUrl: request.SuccessUrl,
            ProviderReference: "noop-provider-reference"));
}
