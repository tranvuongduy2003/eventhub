namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class DiscountCodeRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public required string Code { get; set; }

    public required string Type { get; set; }

    public decimal Value { get; set; }

    public DateTimeOffset? StartAt { get; set; }

    public DateTimeOffset? EndAt { get; set; }

    public int? UsageCap { get; set; }

    public int UsedCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long RowVersion { get; set; }
}
