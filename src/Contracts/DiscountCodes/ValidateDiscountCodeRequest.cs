namespace EventHub.Contracts.DiscountCodes;

public sealed record ValidateDiscountCodeRequest(
    string Code,
    decimal OrderTotalAmount,
    string OrderTotalCurrency);
