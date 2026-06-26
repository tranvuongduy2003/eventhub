using EventHub.Application.Common;

namespace EventHub.Application.DiscountCodes;

public static class DiscountCodeErrors
{
    public static readonly Error EventNotFound = Error.NotFound(
        "DISCOUNT_CODE_EVENT_NOT_FOUND",
        "The event was not found.");

    public static readonly Error NotFound = Error.NotFound(
        "DISCOUNT_CODE_NOT_FOUND",
        "The discount code was not found.");

    public static readonly Error AlreadyExists = Error.Validation(
        "DISCOUNT_CODE_ALREADY_EXISTS",
        "A discount code with this code already exists for this event.");

    public static readonly Error Expired = Error.Validation(
        "DISCOUNT_CODE_EXPIRED",
        "The discount code has expired.");

    public static readonly Error NotYetValid = Error.Validation(
        "DISCOUNT_CODE_NOT_YET_VALID",
        "The discount code is not yet valid.");

    public static readonly Error Exhausted = Error.Validation(
        "DISCOUNT_CODE_EXHAUSTED",
        "The discount code has reached its usage cap.");

    public static readonly Error InvalidPercentage = Error.Validation(
        "DISCOUNT_CODE_INVALID_PERCENTAGE",
        "Percentage discount value must be between 1 and 100.");

    public static readonly Error InvalidAmount = Error.Validation(
        "DISCOUNT_CODE_INVALID_AMOUNT",
        "Fixed-amount discount value must be greater than zero.");

    public static readonly Error InvalidCodeFormat = Error.Validation(
        "DISCOUNT_CODE_INVALID_FORMAT",
        "Discount code must be between 3 and 30 alphanumeric characters.");

    public static readonly Error Deleted = Error.Validation(
        "DISCOUNT_CODE_DELETED",
        "The discount code has been deleted.");

    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "DISCOUNT_CODE_CONCURRENCY_CONFLICT",
        "A concurrency conflict occurred. Please try again.");
}
