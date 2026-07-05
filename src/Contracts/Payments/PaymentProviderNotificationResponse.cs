namespace EventHub.Contracts.Payments;

public sealed record PaymentProviderNotificationResponse(
    int PaymentId,
    int OrderId,
    string PaymentStatus,
    string OrderStatus,
    bool Applied);
