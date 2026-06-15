namespace EventHub.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken);
}

public sealed record PaymentInitiationRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string SuccessUrl,
    string CancelUrl);

public sealed record PaymentInitiationResult(string RedirectUrl, string ProviderReference);
