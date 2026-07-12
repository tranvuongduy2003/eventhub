using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Payments;

public sealed class Payment : AggregateRoot<PaymentId>
{
    private Payment()
    {
    }

    public OrderId OrderId { get; private set; }

    public Money Amount { get; private set; } = null!;

    public PaymentStatus Status { get; private set; }

    public ProviderReference ProviderReference { get; private set; } = null!;

    public DateTimeOffset InitiatedAt { get; private set; }

    public DateTimeOffset? CapturedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public DateTimeOffset? RefundedAt { get; private set; }

    public long RowVersion { get; private set; }

    public static Payment Initiate(
        OrderId orderId,
        Money amount,
        ProviderReference providerReference,
        DateTimeOffset initiatedAt,
        PaymentId? id = null)
    {
        if (amount.Amount <= 0)
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_AMOUNT_REQUIRED",
                "A payment requires a non-zero amount.");
        }

        var payment = new Payment
        {
            Id = id ?? default,
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Initiated,
            ProviderReference = providerReference,
            InitiatedAt = initiatedAt,
            CapturedAt = null,
            FailedAt = null,
            RefundedAt = null,
            RowVersion = 1,
        };

        payment.Raise(new PaymentInitiatedEvent(
            payment.Id,
            orderId,
            amount.Amount,
            amount.Currency,
            providerReference.Value,
            initiatedAt));

        return payment;
    }

    public bool Capture(DateTimeOffset capturedAt)
    {
        if (Status is PaymentStatus.Captured)
        {
            return false;
        }

        if (Status is not PaymentStatus.Initiated)
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_NOT_CAPTURABLE",
                Status switch
                {
                    PaymentStatus.Failed => "Cannot capture a failed payment.",
                    PaymentStatus.Refunded => "Cannot capture a refunded payment.",
                    _ => "The payment cannot be captured in its current status.",
                });
        }

        Status = PaymentStatus.Captured;
        CapturedAt = capturedAt;

        Raise(new PaymentCapturedEvent(Id, OrderId, ProviderReference.Value, capturedAt));
        return true;
    }

    public bool Fail(DateTimeOffset failedAt)
    {
        if (Status is PaymentStatus.Failed)
        {
            return false;
        }

        if (Status is not PaymentStatus.Initiated)
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_NOT_FAILABLE",
                Status switch
                {
                    PaymentStatus.Captured => "Cannot fail a captured payment.",
                    PaymentStatus.Refunded => "Cannot fail a refunded payment.",
                    _ => "The payment cannot be failed in its current status.",
                });
        }

        Status = PaymentStatus.Failed;
        FailedAt = failedAt;

        Raise(new PaymentFailedEvent(Id, OrderId, ProviderReference.Value, failedAt));
        return true;
    }

    public bool Refund(DateTimeOffset refundedAt)
    {
        if (Status is PaymentStatus.Refunded)
        {
            return false;
        }

        if (Status is not PaymentStatus.Captured)
        {
            throw new BusinessRuleValidationException(
                "PAYMENT_NOT_REFUNDABLE",
                Status switch
                {
                    PaymentStatus.Initiated => "Cannot refund a payment that has not been captured.",
                    PaymentStatus.Failed => "Cannot refund a failed payment.",
                    _ => "The payment cannot be refunded in its current status.",
                });
        }

        Status = PaymentStatus.Refunded;
        RefundedAt = refundedAt;

        Raise(new PaymentRefundedEvent(Id, OrderId, ProviderReference.Value, refundedAt));
        return true;
    }

    public static Payment FromPersistence(
        PaymentId id,
        OrderId orderId,
        Money amount,
        PaymentStatus status,
        ProviderReference providerReference,
        DateTimeOffset initiatedAt,
        DateTimeOffset? capturedAt,
        DateTimeOffset? failedAt,
        DateTimeOffset? refundedAt,
        long rowVersion) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            Amount = amount,
            Status = status,
            ProviderReference = providerReference,
            InitiatedAt = initiatedAt,
            CapturedAt = capturedAt,
            FailedAt = failedAt,
            RefundedAt = refundedAt,
            RowVersion = rowVersion,
        };
}
