using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Orders;

public sealed class OrderLineTests
{
    // --- OrderLine.Create ---

    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var ticketTypeId = TicketTypeId.From(1);
        var price = Money.Create(50m, "VND");

        var line = OrderLine.Create(ticketTypeId, 2, price);

        line.TicketTypeId.Should().Be(ticketTypeId);
        line.Quantity.Should().Be(2);
        line.UnitPriceSnapshot.Should().Be(price);
    }

    [Fact]
    public void Create_ValidInput_CalculatesLineTotal()
    {
        var price = Money.Create(50m, "VND");

        var line = OrderLine.Create(TicketTypeId.From(1), 3, price);

        line.LineTotal.Amount.Should().Be(150m);
        line.LineTotal.Currency.Should().Be("VND");
    }

    [Fact]
    public void Create_FreeTicket_LineTotalIsZero()
    {
        var price = Money.Create(0, "VND");

        var line = OrderLine.Create(TicketTypeId.From(1), 5, price);

        line.LineTotal.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_ZeroQuantity_ThrowsBusinessRuleValidationException()
    {
        var act = () => OrderLine.Create(TicketTypeId.From(1), 0, Money.Create(50m, "VND"));

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_LINE_QUANTITY_INVALID");
    }

    [Fact]
    public void Create_NegativeQuantity_ThrowsBusinessRuleValidationException()
    {
        var act = () => OrderLine.Create(TicketTypeId.From(1), -1, Money.Create(50m, "VND"));

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_LINE_QUANTITY_INVALID");
    }

    // --- OrderLine.FromPersistence ---

    [Fact]
    public void FromPersistence_SetsAllProperties()
    {
        var line = OrderLine.FromPersistence(
            OrderLineId.From(10),
            TicketTypeId.From(3),
            4,
            Money.Create(25m, "VND"),
            Money.Create(100m, "VND"));

        line.Id.Value.Should().Be(10);
        line.TicketTypeId.Value.Should().Be(3);
        line.Quantity.Should().Be(4);
        line.UnitPriceSnapshot.Amount.Should().Be(25m);
        line.LineTotal.Amount.Should().Be(100m);
    }

    // --- OrderLineId ---

    [Fact]
    public void OrderLineId_ValidInput_CreatesId()
    {
        var id = OrderLineId.From(1);

        id.Value.Should().Be(1);
    }

    [Fact]
    public void OrderLineId_ZeroValue_ThrowsBusinessRuleValidationException()
    {
        var act = () => OrderLineId.From(0);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_LINE_ID_INVALID");
    }

    [Fact]
    public void OrderLineId_NegativeValue_ThrowsBusinessRuleValidationException()
    {
        var act = () => OrderLineId.From(-1);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_LINE_ID_INVALID");
    }
}
