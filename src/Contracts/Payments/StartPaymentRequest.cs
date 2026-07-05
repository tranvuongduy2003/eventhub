namespace EventHub.Contracts.Payments;

public sealed record StartPaymentRequest(string SuccessUrl, string CancelUrl);
