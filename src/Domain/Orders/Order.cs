using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Orders;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public EventId EventId { get; private set; }

    public Contact Contact { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public Money Total { get; private set; } = null!;

    public int? PaymentId { get; private set; }

    public ReservationId? ReservationId { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public long RowVersion { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Place(
        EventId eventId,
        Contact contact,
        List<OrderLine> lines,
        DateTimeOffset placedAt)
    {
        if (lines.Count == 0)
        {
            throw new BusinessRuleValidationException(
                "ORDER_NO_ITEMS",
                "An order must contain at least one line item.");
        }

        var currency = lines[0].LineTotal.Currency;
        var totalAmount = lines.Sum(l => l.LineTotal.Amount);
        var total = Money.Create(totalAmount, currency);

        var order = new Order
        {
            EventId = eventId,
            Contact = contact,
            Status = OrderStatus.Pending,
            Total = total,
            PaymentId = null,
            PlacedAt = placedAt,
            ConfirmedAt = null,
            ExpiresAt = null,
            CancelledAt = null,
            RowVersion = 1,
        };

        order._lines.AddRange(lines);

        order.Raise(new OrderPlacedEvent(order.Id, eventId, total.Amount, total.Currency, placedAt));

        return order;
    }

    public void MarkConfirmed(int? paymentId, DateTimeOffset confirmedAt)
    {
        if (Status is not OrderStatus.Pending)
        {
            throw new BusinessRuleValidationException(
                "ORDER_NOT_CONFIRMABLE",
                Status switch
                {
                    OrderStatus.Expired => "Cannot confirm an expired order.",
                    OrderStatus.Cancelled => "Cannot confirm a cancelled order.",
                    OrderStatus.Confirmed => "The order is already confirmed.",
                    OrderStatus.Refunded => "Cannot confirm a refunded order.",
                    _ => "The order cannot be confirmed in its current status.",
                });
        }

        // INV-23: confirmation requires a captured payment OR a zero total (free)
        if (Total.Amount > 0 && paymentId is null)
        {
            throw new BusinessRuleValidationException(
                "ORDER_PAYMENT_REQUIRED",
                "A non-zero order requires a captured payment before confirmation.");
        }

        PaymentId = paymentId;
        Status = OrderStatus.Confirmed;
        ConfirmedAt = confirmedAt;

        Raise(new OrderConfirmedEvent(Id, EventId, confirmedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status is not OrderStatus.Pending)
        {
            throw new BusinessRuleValidationException(
                "ORDER_NOT_EXPIRABLE",
                Status switch
                {
                    OrderStatus.Expired => "The order is already expired.",
                    OrderStatus.Confirmed => "Cannot expire a confirmed order.",
                    OrderStatus.Cancelled => "Cannot expire a cancelled order.",
                    _ => "The order cannot be expired in its current status.",
                });
        }

        Status = OrderStatus.Expired;
        ExpiresAt = expiredAt;

        Raise(new OrderExpiredEvent(Id, expiredAt));
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is OrderStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "ORDER_ALREADY_CANCELLED",
                "The order is already cancelled.");
        }

        if (Status is OrderStatus.Refunded)
        {
            throw new BusinessRuleValidationException(
                "ORDER_NOT_CANCELLABLE",
                "Cannot cancel a refunded order.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = cancelledAt;

        Raise(new OrderCancelledEvent(Id, cancelledAt));
    }

    public void SetReservationId(ReservationId reservationId)
    {
        ReservationId = reservationId;
    }

    public void LoadLines(List<OrderLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
    }

    public static Order FromPersistence(
        OrderId id,
        EventId eventId,
        Contact contact,
        OrderStatus status,
        Money total,
        int? paymentId,
        ReservationId? reservationId,
        DateTimeOffset placedAt,
        DateTimeOffset? confirmedAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset? cancelledAt,
        long rowVersion) =>
        new()
        {
            Id = id,
            EventId = eventId,
            Contact = contact,
            Status = status,
            Total = total,
            PaymentId = paymentId,
            ReservationId = reservationId,
            PlacedAt = placedAt,
            ConfirmedAt = confirmedAt,
            ExpiresAt = expiresAt,
            CancelledAt = cancelledAt,
            RowVersion = rowVersion,
        };
}
