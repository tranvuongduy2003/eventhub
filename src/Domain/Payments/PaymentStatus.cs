namespace EventHub.Domain.Payments;

public enum PaymentStatus
{
    Initiated = 0,
    Captured = 1,
    Failed = 2,
    Refunded = 3,
}
