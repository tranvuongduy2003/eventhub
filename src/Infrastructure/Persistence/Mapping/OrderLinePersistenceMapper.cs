using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class OrderLinePersistenceMapper
{
    public static OrderLineRecord ToRecord(OrderLine domain, int orderId) =>
        new()
        {
            Id = domain.Id.Value,
            OrderId = orderId,
            TicketTypeId = domain.TicketTypeId.Value,
            Quantity = domain.Quantity,
            UnitPriceAmount = domain.UnitPriceSnapshot.Amount,
            UnitPriceCurrency = domain.UnitPriceSnapshot.Currency,
            LineTotalAmount = domain.LineTotal.Amount,
            LineTotalCurrency = domain.LineTotal.Currency,
        };

    public static OrderLine ToDomain(OrderLineRecord record) =>
        OrderLine.FromPersistence(
            OrderLineId.From(record.Id),
            TicketTypeId.From(record.TicketTypeId),
            record.Quantity,
            Money.Create(record.UnitPriceAmount, record.UnitPriceCurrency),
            Money.Create(record.LineTotalAmount, record.LineTotalCurrency));
}
