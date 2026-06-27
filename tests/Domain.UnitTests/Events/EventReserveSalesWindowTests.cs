using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventReserveSalesWindowTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartsAt = new(2026, 7, 15, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = new(2026, 7, 15, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowStart = new(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 7, 20, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reserve_SalesWindowOpen_Succeeds()
    {
        var publishedEvent = CreatePublishedEventWithSalesWindow(WindowStart, WindowEnd);
        var ticketTypeId = publishedEvent.TicketTypes.First().Id;
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

        var reservation = publishedEvent.Reserve(
            ticketTypeId, 2, OrderId.From(1), now.AddHours(1), now);

        reservation.Should().NotBeNull();
        reservation.Quantity.Should().Be(2);
    }

    [Fact]
    public void Reserve_NoSalesWindow_Succeeds()
    {
        var publishedEvent = CreatePublishedEventWithSalesWindow(null, null);
        var ticketTypeId = publishedEvent.TicketTypes.First().Id;
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

        var reservation = publishedEvent.Reserve(
            ticketTypeId, 2, OrderId.From(1), now.AddHours(1), now);

        reservation.Should().NotBeNull();
    }

    [Fact]
    public void Reserve_SalesWindowNotOpen_BeforeStart_ThrowsBusinessRuleValidationException()
    {
        var publishedEvent = CreatePublishedEventWithSalesWindow(WindowStart, WindowEnd);
        var ticketTypeId = publishedEvent.TicketTypes.First().Id;
        var now = new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);

        var act = () => publishedEvent.Reserve(
            ticketTypeId, 2, OrderId.From(1), now.AddHours(1), now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("SALES_WINDOW_NOT_OPEN");
    }

    [Fact]
    public void Reserve_SalesWindowNotOpen_AfterEnd_ThrowsBusinessRuleValidationException()
    {
        var publishedEvent = CreatePublishedEventWithSalesWindow(WindowStart, WindowEnd);
        var ticketTypeId = publishedEvent.TicketTypes.First().Id;
        var now = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

        var act = () => publishedEvent.Reserve(
            ticketTypeId, 2, OrderId.From(1), now.AddHours(1), now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("SALES_WINDOW_NOT_OPEN");
    }

    [Fact]
    public void Reserve_EventNotPublished_ThrowsBusinessRuleValidationException()
    {
        var draftEvent = CreateValidDraftEvent();
        var ticketType = draftEvent.AddTicketType(
            TicketName.Create("General"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            null,
            CreatedAt);

        var act = () => draftEvent.Reserve(
            ticketType.Id, 2, OrderId.From(1), CreatedAt.AddHours(1), CreatedAt);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_NOT_PUBLISHED");
    }

    // --- TicketType with SalesWindow ---

    [Fact]
    public void TicketType_Create_WithSalesWindow_SetsProperty()
    {
        var window = SalesWindow.Create(WindowStart, WindowEnd);
        var ticketType = TicketType.Create(
            TicketTypeId.From(1),
            TicketName.Create("VIP"),
            Money.Create(100m, "VND"),
            Capacity.Create(50),
            null,
            window,
            CreatedAt);

        ticketType.SalesWindow.Should().Be(window);
    }

    [Fact]
    public void TicketType_Create_WithoutSalesWindow_SetsNull()
    {
        var ticketType = TicketType.Create(
            TicketTypeId.From(1),
            TicketName.Create("General"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            null,
            CreatedAt);

        ticketType.SalesWindow.Should().BeNull();
    }

    [Fact]
    public void TicketType_Update_WithSalesWindow_UpdatesProperty()
    {
        var ticketType = TicketType.Create(
            TicketTypeId.From(1),
            TicketName.Create("General"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            null,
            CreatedAt);

        var window = SalesWindow.Create(WindowStart, WindowEnd);
        ticketType.Update(
            TicketName.Create("General"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            window,
            CreatedAt);

        ticketType.SalesWindow.Should().Be(window);
    }

    [Fact]
    public void TicketType_Update_WithoutSalesWindow_ClearsProperty()
    {
        var window = SalesWindow.Create(WindowStart, WindowEnd);
        var ticketType = TicketType.Create(
            TicketTypeId.From(1),
            TicketName.Create("VIP"),
            Money.Create(100m, "VND"),
            Capacity.Create(50),
            null,
            window,
            CreatedAt);

        ticketType.Update(
            TicketName.Create("VIP"),
            Money.Create(100m, "VND"),
            Capacity.Create(50),
            null,
            null,
            CreatedAt);

        ticketType.SalesWindow.Should().BeNull();
    }

    // --- EditTicketType on Published event with SalesWindow ---

    [Fact]
    public void EditTicketType_PublishedEvent_SalesWindowOnly_Succeeds()
    {
        var publishedEvent = CreatePublishedEventWithSalesWindow(null, null);
        var ticketType = publishedEvent.TicketTypes.First();

        var window = SalesWindow.Create(WindowStart, WindowEnd);
        publishedEvent.EditTicketType(
            ticketType.Id,
            ticketType.Name,
            ticketType.Price,
            ticketType.Capacity,
            null,
            window,
            CreatedAt);

        publishedEvent.TicketTypes.First().SalesWindow.Should().Be(window);
    }

    // --- Helper methods ---

    private static Event CreateValidDraftEvent() =>
        Event.CreateDraft(
            UserId.New(),
            EventTitle.Create("Tech Conference 2026"),
            EventSchedule.Create(StartsAt, EndsAt, "UTC"),
            EventLocation.Create("123 Conference Ave", false),
            CreatedAt);

    private static Event CreatePublishedEventWithSalesWindow(DateTimeOffset? start, DateTimeOffset? end)
    {
        var draftEvent = CreateValidDraftEvent();

        SalesWindow? window = null;
        if (start.HasValue && end.HasValue)
        {
            window = SalesWindow.Create(start.Value, end.Value);
        }

        draftEvent.AddTicketType(
            TicketName.Create("General"),
            Money.Create(50m, "VND"),
            Capacity.Create(100),
            null,
            window,
            CreatedAt);

        draftEvent.Publish(Slug.Create("tech-conference-2026"), CreatedAt);
        return draftEvent;
    }
}
