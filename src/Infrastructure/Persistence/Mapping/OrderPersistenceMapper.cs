using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class OrderPersistenceMapper
{
    public static OrderRecord ToRecord(Order domain) =>
        new()
        {
            Id = domain.Id.Value,
            EventId = domain.EventId.Value,
            ContactName = domain.Contact.Name,
            ContactEmail = domain.Contact.Email,
            Status = domain.Status.ToString(),
            TotalAmount = domain.Total.Amount,
            TotalCurrency = domain.Total.Currency,
            PaymentId = domain.PaymentId,
            ReservationId = domain.ReservationId?.Value,
            DiscountCodeId = domain.DiscountCodeId?.Value,
            DiscountAmount = domain.DiscountAmount?.Amount,
            PlacedAt = domain.PlacedAt,
            ConfirmedAt = domain.ConfirmedAt,
            ExpiresAt = domain.ExpiresAt,
            CancelledAt = domain.CancelledAt,
            RowVersion = domain.RowVersion,
            Lines = domain.Lines.Select(l => OrderLinePersistenceMapper.ToRecord(l, domain.Id.Value)).ToList(),
        };

    public static Order ToDomain(OrderRecord record)
    {
        var contact = Contact.Create(record.ContactName, record.ContactEmail);
        var total = Money.Create(record.TotalAmount, record.TotalCurrency);

        var status = Enum.Parse<OrderStatus>(record.Status);

        var reservationId = record.ReservationId.HasValue
            ? ReservationId.From(record.ReservationId.Value)
            : (ReservationId?)null;

        var discountCodeId = record.DiscountCodeId.HasValue
            ? DiscountCodeId.From(record.DiscountCodeId.Value)
            : (DiscountCodeId?)null;

        var discountAmount = record.DiscountAmount.HasValue
            ? Money.Create(record.DiscountAmount.Value, record.TotalCurrency)
            : null;

        var order = Order.FromPersistence(
            OrderId.From(record.Id),
            EventId.From(record.EventId),
            contact,
            status,
            total,
            record.PaymentId,
            reservationId,
            discountCodeId,
            discountAmount,
            record.PlacedAt,
            record.ConfirmedAt,
            record.ExpiresAt,
            record.CancelledAt,
            record.RowVersion);

        var lines = record.Lines.Select(OrderLinePersistenceMapper.ToDomain).ToList();
        order.LoadLines(lines);

        return order;
    }
}
