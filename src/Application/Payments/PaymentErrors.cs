using EventHub.Application.Common;

namespace EventHub.Application.Payments;

public static class PaymentErrors
{
    public static readonly Error OrderNotFound = Error.NotFound(
        "ORDER_NOT_FOUND",
        "The order was not found.");

    public static readonly Error PaymentNotFound = Error.NotFound(
        "PAYMENT_NOT_FOUND",
        "The payment was not found.");

    public static readonly Error PaymentNotRequired = Error.Validation(
        "PAYMENT_NOT_REQUIRED",
        "This order does not require payment.");

    public static readonly Error OrderNotPayable = Error.Validation(
        "ORDER_NOT_PAYABLE",
        "This order cannot be paid in its current status.");

    public static readonly Error PaymentAmountMismatch = Error.Validation(
        "PAYMENT_AMOUNT_MISMATCH",
        "The payment amount does not match the order total.");

    public static readonly Error OrderReservationNotFound = Error.Validation(
        "ORDER_RESERVATION_NOT_FOUND",
        "The order no longer has an active inventory hold.");

    public static readonly Error OrderEventNotFound = Error.NotFound(
        "ORDER_EVENT_NOT_FOUND",
        "The event for this order was not found.");
}
