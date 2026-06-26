namespace EventHub.Contracts.DiscountCodes;

public sealed record UpdateDiscountCodeRequest(
    string Type,
    decimal Value,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int? UsageCap);
