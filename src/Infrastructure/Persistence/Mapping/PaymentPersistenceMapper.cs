using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class PaymentPersistenceMapper
{
    public static PaymentRecord ToRecord(Payment domain) =>
        new()
        {
            Id = domain.Id.Value,
            OrderId = domain.OrderId.Value,
            Amount = domain.Amount.Amount,
            Currency = domain.Amount.Currency,
            Status = domain.Status.ToString(),
            ProviderReference = domain.ProviderReference.Value,
            InitiatedAt = domain.InitiatedAt,
            CapturedAt = domain.CapturedAt,
            FailedAt = domain.FailedAt,
            RefundedAt = domain.RefundedAt,
            RowVersion = domain.RowVersion,
        };

    public static Payment ToDomain(PaymentRecord record) =>
        Payment.FromPersistence(
            PaymentId.From(record.Id),
            OrderId.From(record.OrderId),
            Money.Create(record.Amount, record.Currency),
            Enum.Parse<PaymentStatus>(record.Status),
            ProviderReference.Create(record.ProviderReference),
            record.InitiatedAt,
            record.CapturedAt,
            record.FailedAt,
            record.RefundedAt,
            record.RowVersion);
}
