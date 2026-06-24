using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

// OrderLine is a child entity of the Order aggregate

namespace EventHub.Domain.Orders;

public sealed class OrderLine : Entity<OrderLineId>
{
    private OrderLine()
    {
    }

    public TicketTypeId TicketTypeId { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPriceSnapshot { get; private set; } = null!;

    public Money LineTotal { get; private set; } = null!;

    public static OrderLine Create(
        TicketTypeId ticketTypeId,
        int quantity,
        Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException(
                "ORDER_LINE_QUANTITY_INVALID",
                "Order line quantity must be at least 1.");
        }

        var lineTotal = Money.Create(unitPrice.Amount * quantity, unitPrice.Currency);

        return new OrderLine
        {
            TicketTypeId = ticketTypeId,
            Quantity = quantity,
            UnitPriceSnapshot = unitPrice,
            LineTotal = lineTotal,
        };
    }

    public static OrderLine FromPersistence(
        OrderLineId id,
        TicketTypeId ticketTypeId,
        int quantity,
        Money unitPriceSnapshot,
        Money lineTotal) =>
        new()
        {
            Id = id,
            TicketTypeId = ticketTypeId,
            Quantity = quantity,
            UnitPriceSnapshot = unitPriceSnapshot,
            LineTotal = lineTotal,
        };
}
