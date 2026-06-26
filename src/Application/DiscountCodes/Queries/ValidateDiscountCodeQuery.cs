using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.DiscountCodes.Queries;

public sealed record ValidateDiscountCodeQuery(
    int EventId,
    string Code,
    decimal OrderTotalAmount,
    string OrderTotalCurrency)
    : IQuery<ValidateDiscountCodeResult>;

public sealed record ValidateDiscountCodeResult(
    int DiscountCodeId,
    string Code,
    string Type,
    decimal Value,
    decimal DiscountAmount,
    decimal FinalTotal,
    string Currency);
