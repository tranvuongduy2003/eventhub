namespace EventHub.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken);
}

public sealed record PaymentInitiationRequest(
    int OrderId,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl,
    string? ExistingProviderReference);

public sealed record PaymentInitiationResult(string RedirectUrl, string ProviderReference);
