namespace EventHub.Contracts.DiscountCodes;

public sealed record CreateDiscountCodeRequest(
    string Code,
    string Type,
    decimal Value,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int? UsageCap);
