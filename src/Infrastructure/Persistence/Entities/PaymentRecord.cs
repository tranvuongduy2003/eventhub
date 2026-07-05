namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class PaymentRecord
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string Status { get; set; }

    public required string ProviderReference { get; set; }

    public DateTimeOffset InitiatedAt { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public DateTimeOffset? RefundedAt { get; set; }

    public long RowVersion { get; set; }
}
