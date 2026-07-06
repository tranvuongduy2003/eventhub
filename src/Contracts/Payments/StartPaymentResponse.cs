namespace EventHub.Contracts.Payments;

public sealed record StartPaymentResponse(
    int PaymentId,
    int OrderId,
    decimal Amount,
    string Currency,
    string ProviderReference,
    string RedirectUrl);
