namespace EventHub.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken);

    Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest request,
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

public sealed record PaymentRefundRequest(
    int PaymentId,
    int OrderId,
    decimal Amount,
    string Currency,
    string ProviderReference);

public sealed record PaymentRefundResult(string ProviderReference, bool Applied);
