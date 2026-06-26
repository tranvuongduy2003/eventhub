using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Orders;

public sealed class OrderTests
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly EventId TestEventId = EventId.From(1);

    // --- Order.Place ---

    [Fact]
    public void Place_FreeOrder_TotalIsZero()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Total.Amount.Should().Be(0);
    }

    [Fact]
    public void Place_FreeOrder_StatusIsPending()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Place_FreeOrder_RaisesOrderPlacedEvent()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlacedEvent>();
    }

    [Fact]
    public void Place_PaidOrder_TotalIsSumOfLineTotals()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(50m, "VND")),
            OrderLine.Create(TicketTypeId.From(2), 1, Money.Create(100m, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Total.Amount.Should().Be(200m);
    }

    [Fact]
    public void Place_MixedFreeAndPaid_TotalIsPaidAmountOnly()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(0, "VND")),
            OrderLine.Create(TicketTypeId.From(2), 1, Money.Create(100m, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Total.Amount.Should().Be(100m);
    }

    [Fact]
    public void Place_EmptyLines_ThrowsBusinessRuleValidationException()
    {
        var act = () => Order.Place(TestEventId, CreateContact(), [], PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_NO_ITEMS");
    }

    [Fact]
    public void Place_SetsContact()
    {
        var contact = Contact.Create("Jane Doe", "jane@example.com");
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, contact, lines, PlacedAt);

        order.Contact.Should().Be(contact);
    }

    [Fact]
    public void Place_SetsEventId()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.EventId.Should().Be(TestEventId);
    }

    [Fact]
    public void Place_SetsPlacedAt()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.PlacedAt.Should().Be(PlacedAt);
    }

    [Fact]
    public void Place_PaymentIdIsNull()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.PaymentId.Should().BeNull();
    }

    [Fact]
    public void Place_SetsLines()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, Money.Create(50m, "VND")),
            OrderLine.Create(TicketTypeId.From(2), 1, Money.Create(100m, "VND")),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Lines.Should().HaveCount(2);
    }

    // --- Order.MarkConfirmed ---

    [Fact]
    public void MarkConfirmed_FreeOrder_SucceedsWithoutPaymentId()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.PaymentId.Should().BeNull();
        order.ConfirmedAt.Should().Be(PlacedAt);
    }

    [Fact]
    public void MarkConfirmed_FreeOrder_RaisesOrderConfirmedEvent()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.ClearDomainEvents();

        order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderConfirmedEvent>();
    }

    [Fact]
    public void MarkConfirmed_PaidOrderWithPayment_Succeeds()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(100m, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.MarkConfirmed(paymentId: 42, confirmedAt: PlacedAt);

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.PaymentId.Should().Be(42);
    }

    [Fact]
    public void MarkConfirmed_PaidOrderWithoutPayment_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(100m, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        var act = () => order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_PAYMENT_REQUIRED");
    }

    [Fact]
    public void MarkConfirmed_AlreadyConfirmed_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        var act = () => order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_NOT_CONFIRMABLE");
    }

    [Fact]
    public void MarkConfirmed_ExpiredOrder_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.Expire(PlacedAt);

        var act = () => order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_NOT_CONFIRMABLE");
    }

    [Fact]
    public void MarkConfirmed_CancelledOrder_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.Cancel(PlacedAt);

        var act = () => order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_NOT_CONFIRMABLE");
    }

    // --- Order.Expire ---

    [Fact]
    public void Expire_PendingOrder_TransitionsToExpired()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Expire(PlacedAt);

        order.Status.Should().Be(OrderStatus.Expired);
        order.ExpiresAt.Should().Be(PlacedAt);
    }

    [Fact]
    public void Expire_PendingOrder_RaisesOrderExpiredEvent()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.ClearDomainEvents();

        order.Expire(PlacedAt);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderExpiredEvent>();
    }

    [Fact]
    public void Expire_ConfirmedOrder_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        var act = () => order.Expire(PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_NOT_EXPIRABLE");
    }

    [Fact]
    public void Expire_AlreadyExpired_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.Expire(PlacedAt);

        var act = () => order.Expire(PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_NOT_EXPIRABLE");
    }

    // --- Order.Cancel ---

    [Fact]
    public void Cancel_PendingOrder_TransitionsToCancelled()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        order.Cancel(PlacedAt);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(PlacedAt);
    }

    [Fact]
    public void Cancel_PendingOrder_RaisesOrderCancelledEvent()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.ClearDomainEvents();

        order.Cancel(PlacedAt);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCancelledEvent>();
    }

    [Fact]
    public void Cancel_ConfirmedOrder_Succeeds()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.MarkConfirmed(paymentId: null, confirmedAt: PlacedAt);

        order.Cancel(PlacedAt);

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsBusinessRuleValidationException()
    {
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 1, Money.Create(0, "VND")),
        };
        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);
        order.Cancel(PlacedAt);

        var act = () => order.Cancel(PlacedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_ALREADY_CANCELLED");
    }

    // --- INV-25: Price snapshotting ---

    [Fact]
    public void Place_SnapshotsPriceAtPlacementTime()
    {
        var originalPrice = Money.Create(50m, "VND");
        var lines = new List<OrderLine>
        {
            OrderLine.Create(TicketTypeId.From(1), 2, originalPrice),
        };

        var order = Order.Place(TestEventId, CreateContact(), lines, PlacedAt);

        // The order line should have the snapshotted price, not a reference to the original
        order.Lines.First().UnitPriceSnapshot.Amount.Should().Be(50m);
        order.Lines.First().LineTotal.Amount.Should().Be(100m);
    }

    // --- Order.FromPersistence ---

    [Fact]
    public void FromPersistence_SetsAllProperties()
    {
        var contact = Contact.Create("Jane Doe", "jane@example.com");
        var total = Money.Create(0, "VND");

        var order = Order.FromPersistence(
            OrderId.From(42),
            TestEventId,
            contact,
            OrderStatus.Confirmed,
            total,
            paymentId: null,
            reservationId: null,
            discountCodeId: null,
            discountAmount: null,
            PlacedAt,
            confirmedAt: PlacedAt,
            expiresAt: null,
            cancelledAt: null,
            rowVersion: 1);

        order.Id.Value.Should().Be(42);
        order.EventId.Should().Be(TestEventId);
        order.Contact.Should().Be(contact);
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.Total.Should().Be(total);
        order.PaymentId.Should().BeNull();
        order.PlacedAt.Should().Be(PlacedAt);
        order.ConfirmedAt.Should().Be(PlacedAt);
        order.ExpiresAt.Should().BeNull();
        order.CancelledAt.Should().BeNull();
        order.RowVersion.Should().Be(1);
    }

    // --- OrderId ---

    [Fact]
    public void OrderId_ValidInput_CreatesId()
    {
        var id = OrderId.From(1);

        id.Value.Should().Be(1);
    }

    [Fact]
    public void OrderId_ZeroValue_ThrowsBusinessRuleValidationException()
    {
        var act = () => OrderId.From(0);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_ID_INVALID");
    }

    [Fact]
    public void OrderId_NegativeValue_ThrowsBusinessRuleValidationException()
    {
        var act = () => OrderId.From(-1);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("ORDER_ID_INVALID");
    }

    // --- Helper methods ---

    private static Contact CreateContact() =>
        Contact.Create("John Doe", "john@example.com");
}
