using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.DiscountCodes.Queries;

public sealed record GetDiscountCodesQuery(int EventId)
    : IQuery<List<DiscountCodeResult>>;

public sealed record DiscountCodeResult(
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
