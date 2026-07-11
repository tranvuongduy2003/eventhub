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

    [Fact]
    public void CheckIn_WhenTicketIsValidForEvent_MarksCheckedInAndRaisesEvent()
    {
        var ticket = CreateTicket();
        var checkedInAt = new DateTimeOffset(2026, 7, 10, 18, 0, 0, TimeSpan.Zero);

        ticket.ClearDomainEvents();
        ticket.CheckIn(EventId.From(1), checkedInAt);

        ticket.Status.Should().Be(TicketStatus.CheckedIn);
        ticket.CheckedInAt.Should().Be(checkedInAt);
        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketCheckedInEvent>()
            .Which.CheckedInAt.Should().Be(checkedInAt);
    }

    [Fact]
    public void CheckIn_WhenAlreadyCheckedIn_ThrowsWithFirstCheckInTime()
    {
        var ticket = CreateTicket();
        var firstCheckedInAt = new DateTimeOffset(2026, 7, 10, 18, 0, 0, TimeSpan.Zero);

        ticket.CheckIn(EventId.From(1), firstCheckedInAt);

        var act = () => ticket.CheckIn(EventId.From(1), firstCheckedInAt.AddMinutes(1));

        act.Should().Throw<BusinessRuleValidationException>()
            .Where(exception => exception.Code == "TICKET_ALREADY_CHECKED_IN")
            .WithMessage($"*{firstCheckedInAt:O}*");
    }

    [Fact]
    public void CheckIn_WhenTicketBelongsToDifferentEvent_Throws()
    {
        var ticket = CreateTicket();

        var act = () => ticket.CheckIn(EventId.From(99), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleValidationException>()
            .Where(exception => exception.Code == "TICKET_WRONG_EVENT");
    }

    [Fact]
    public void Transfer_WhenTicketIsValid_TransfersSourceAndIssuesReplacement()
    {
        var ticket = CreateTicket();
        var transferredAt = new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero);
        var recipient = Contact.Create("Recipient Friend", "Recipient@Example.com");
        var replacementCode = TicketCode.Create("tk_bcdefghijklmnopqrstuvwxyz1234567");

        ticket.ClearDomainEvents();
        var replacement = ticket.Transfer(recipient, replacementCode, transferredAt, TicketId.From(5));

        ticket.Status.Should().Be(TicketStatus.Transferred);
        replacement.Status.Should().Be(TicketStatus.Valid);
        replacement.Id.Should().Be(TicketId.From(5));
        replacement.EventId.Should().Be(ticket.EventId);
        replacement.OrderId.Should().Be(ticket.OrderId);
        replacement.TicketTypeId.Should().Be(ticket.TicketTypeId);
        replacement.Code.Should().Be(replacementCode);
        replacement.Holder.Email.Should().Be("recipient@example.com");
        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketTransferredEvent>()
            .Which.ReplacementCode.Should().Be(replacementCode);
        replacement.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketIssuedEvent>();
    }

    [Fact]
    public void Transfer_WhenTicketIsCheckedIn_Throws()
    {
        var ticket = CreateTicket();
        ticket.CheckIn(EventId.From(1), new DateTimeOffset(2026, 7, 10, 18, 0, 0, TimeSpan.Zero));

        var act = () => ticket.Transfer(
            Contact.Create("Recipient Friend", "recipient@example.com"),
            TicketCode.Create("tk_bcdefghijklmnopqrstuvwxyz1234567"),
            DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleValidationException>()
            .Where(exception => exception.Code == "TICKET_ALREADY_CHECKED_IN");
    }

    [Fact]
    public void Transfer_WhenTicketAlreadyTransferred_Throws()
    {
        var ticket = CreateTicket();
        ticket.Transfer(
            Contact.Create("Recipient Friend", "recipient@example.com"),
            TicketCode.Create("tk_bcdefghijklmnopqrstuvwxyz1234567"),
            new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero));

        var act = () => ticket.Transfer(
            Contact.Create("Second Recipient", "second@example.com"),
            TicketCode.Create("tk_cdefghijklmnopqrstuvwxyz12345678"),
            DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleValidationException>()
            .Where(exception => exception.Code == "TICKET_NOT_VALID_FOR_TRANSFER");
    }

    [Fact]
    public void CheckIn_WhenTicketIsTransferred_Throws()
    {
        var ticket = CreateTicket();
        ticket.Transfer(
            Contact.Create("Recipient Friend", "recipient@example.com"),
            TicketCode.Create("tk_bcdefghijklmnopqrstuvwxyz1234567"),
            new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero));

        var act = () => ticket.CheckIn(EventId.From(1), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleValidationException>()
            .Where(exception => exception.Code == "TICKET_NOT_VALID_FOR_CHECK_IN");
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
