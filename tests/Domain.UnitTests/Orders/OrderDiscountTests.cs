using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Orders;

public sealed class OrderDiscountTests
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly EventId TestEventId = EventId.From(1);

    // --- Order.Place with discount ---

    [Fact]
    public void Place_WithPercentageDiscount_ReducesTotal()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(50000m, "VND")),
        };
        var discountCodeId = DiscountCodeId.From(1);
        var discountAmount = Money.Create(20000m, "VND");

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt, discountCodeId, discountAmount);

        order.Total.Amount.Should().Be(80000m);
        order.DiscountCodeId.Should().Be(discountCodeId);
        order.DiscountAmount.Should().NotBeNull();
        order.DiscountAmount!.Amount.Should().Be(20000m);
    }

    [Fact]
    public void Place_WithFixedDiscount_ReducesTotal()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(100000m, "VND")),
        };
        var discountCodeId = DiscountCodeId.From(1);
        var discountAmount = Money.Create(30000m, "VND");

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt, discountCodeId, discountAmount);

        order.Total.Amount.Should().Be(70000m);
    }

    [Fact]
    public void Place_DiscountExceedsTotal_ClampsAtZero()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(50000m, "VND")),
        };
        var discountCodeId = DiscountCodeId.From(1);
        var discountAmount = Money.Create(100000m, "VND");

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt, discountCodeId, discountAmount);

        order.Total.Amount.Should().Be(0);
        order.DiscountAmount!.Amount.Should().Be(50000m);
    }

    [Fact]
    public void Place_100PercentDiscount_ReducesToZero()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(50000m, "VND")),
        };
        var discountCodeId = DiscountCodeId.From(1);
        var discountAmount = Money.Create(100000m, "VND");

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt, discountCodeId, discountAmount);

        order.Total.Amount.Should().Be(0);
    }

    [Fact]
    public void Place_NoDiscount_TotalIsSumOfLines()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(50000m, "VND")),
            OrderLine.Create(TicketTypeId.From(2), 1, Money.Create(100000m, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Total.Amount.Should().Be(200000m);
        order.DiscountCodeId.Should().BeNull();
        order.DiscountAmount.Should().BeNull();
    }

    [Fact]
    public void Place_WithDiscount_RaisesOrderPlacedEventWithDiscountFields()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(100000m, "VND")),
        };
        var discountCodeId = DiscountCodeId.From(1);
        var discountAmount = Money.Create(20000m, "VND");

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt, discountCodeId, discountAmount);

        var evt = order.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrderPlacedEvent>().Subject;
        evt.DiscountCodeId.Should().Be(discountCodeId);
        evt.DiscountAmount.Should().Be(20000m);
        evt.TotalAmount.Should().Be(80000m);
    }

    [Fact]
    public void Place_WithDiscount_FreeOrderAutoConfirmStillWorks()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(50000m, "VND")),
        };
        var discountCodeId = DiscountCodeId.From(1);
        var discountAmount = Money.Create(50000m, "VND");

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt, discountCodeId, discountAmount);

        // Total is 0, so free-order auto-confirm should work
        order.Total.Amount.Should().Be(0);
        order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    // --- Order.FromPersistence with discount ---

    [Fact]
    public void FromPersistence_WithDiscount_SetsDiscountFields()
    {
        var contact = Contact.Create("Jane Doe", "jane@example.com");
        var total = Money.Create(80000m, "VND");
        var discountAmount = Money.Create(20000m, "VND");

        var order = Order.FromPersistence(
            OrderId.From(42),
            TestEventId,
            contact,
            OrderStatus.Confirmed,
            total,
            paymentId: null,
            reservationId: null,
            DiscountCodeId.From(1),
            discountAmount,
            PlacedAt,
            confirmedAt: PlacedAt,
            expiresAt: null,
            cancelledAt: null,
            rowVersion: 1);

        order.DiscountCodeId.Should().Be(DiscountCodeId.From(1));
        order.DiscountAmount.Should().NotBeNull();
        order.DiscountAmount!.Amount.Should().Be(20000m);
        order.Total.Amount.Should().Be(80000m);
    }

    private static Contact CreateContact() =>
        Contact.Create("John Doe", "john@example.com");
}
