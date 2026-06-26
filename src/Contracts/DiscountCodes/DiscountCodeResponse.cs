namespace EventHub.Contracts.DiscountCodes;

public sealed record DiscountCodeResponse(
    int DiscountCodeId,
    int EventId,
    string Code,
    string Type,
    decimal Value,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int? UsageCap,
    int UsedCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
