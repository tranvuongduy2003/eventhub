namespace EventHub.Contracts.DiscountCodes;

public sealed record DiscountCodeValidationResponse(
    int DiscountCodeId,
    string Code,
    string Type,
    decimal Value,
    decimal DiscountAmount,
    decimal FinalTotal,
    string Currency);
