namespace EventHub.Domain.Orders;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Expired,
    Cancelled,
    Refunded,
}
