using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Tickets;

public sealed class TicketTests
{
    [Fact]
    public void Issue_CreatesValidTicketAndRaisesIssuedEvent()
    {
        var issuedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

        var ticket = Ticket.Issue(
            EventId.From(1),
            OrderId.From(2),
            TicketTypeId.From(3),
            Contact.Create("Jane Attendee", "Jane@Example.com"),
            TicketCode.Create("tk_abcdefghijklmnopqrstuvwxyz123456"),
            issuedAt,
            TicketId.From(4));

        ticket.Status.Should().Be(TicketStatus.Valid);
        ticket.Holder.Email.Should().Be("jane@example.com");
        ticket.CheckedInAt.Should().BeNull();
        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketIssuedEvent>()
            .Which.Code.Value.Should().Be("tk_abcdefghijklmnopqrstuvwxyz123456");
    }

    [Fact]
    public void MarkDelivered_RecordsDeliveryTimestampWithoutChangingStatus()
    {
        var ticket = CreateTicket();
        var deliveredAt = new DateTimeOffset(2026, 7, 10, 9, 5, 0, TimeSpan.Zero);

        ticket.MarkDelivered(deliveredAt);

        ticket.LastDeliveredAt.Should().Be(deliveredAt);
        ticket.Status.Should().Be(TicketStatus.Valid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("tk_invalid code with spaces 123456789")]
    public void TicketCode_Create_WhenInvalid_Throws(string code)
    {
        var act = () => TicketCode.Create(code);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    private static Ticket CreateTicket() =>
        Ticket.Issue(
            EventId.From(1),
            OrderId.From(2),
            TicketTypeId.From(3),
            Contact.Create("Jane Attendee", "jane@example.com"),
            TicketCode.Create("tk_abcdefghijklmnopqrstuvwxyz123456"),
            new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
            TicketId.From(4));
}
