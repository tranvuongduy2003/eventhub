using EventHub.Domain.Abstractions;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;

namespace EventHub.Domain.Tickets;

public sealed class Ticket : AggregateRoot<TicketId>
{
    private Ticket()
    {
    }

    public EventId EventId { get; private set; }

    public OrderId OrderId { get; private set; }

    public TicketTypeId TicketTypeId { get; private set; }

    public TicketCode Code { get; private set; } = null!;

    public Contact Holder { get; private set; } = null!;

    public TicketStatus Status { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    public DateTimeOffset? LastDeliveredAt { get; private set; }

    public long RowVersion { get; private set; }

    public static Ticket Issue(
        EventId eventId,
        OrderId orderId,
        TicketTypeId ticketTypeId,
        Contact holder,
        TicketCode code,
        DateTimeOffset issuedAt,
        TicketId? id = null)
    {
        var ticket = new Ticket
        {
            Id = id ?? default,
            EventId = eventId,
            OrderId = orderId,
            TicketTypeId = ticketTypeId,
            Holder = holder,
            Code = code,
            Status = TicketStatus.Valid,
            IssuedAt = issuedAt,
            CheckedInAt = null,
            LastDeliveredAt = null,
            RowVersion = 1,
        };

        ticket.Raise(new TicketIssuedEvent(ticket.Id, eventId, orderId, code, issuedAt));

        return ticket;
    }

    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        LastDeliveredAt = deliveredAt;
    }

    public void CheckIn(EventId eventId, DateTimeOffset checkedInAt)
    {
        if (EventId != eventId)
        {
            throw new BusinessRuleValidationException(
                "TICKET_WRONG_EVENT",
                "This ticket is for a different event.");
        }

        if (Status == TicketStatus.CheckedIn)
        {
            throw new BusinessRuleValidationException(
                "TICKET_ALREADY_CHECKED_IN",
                $"This ticket was already checked in at {CheckedInAt:O}.");
        }

        if (Status != TicketStatus.Valid)
        {
            throw new BusinessRuleValidationException(
                "TICKET_NOT_VALID_FOR_CHECK_IN",
                "This ticket is not valid for check-in.");
        }

        Status = TicketStatus.CheckedIn;
        CheckedInAt = checkedInAt;

        Raise(new TicketCheckedInEvent(Id, EventId, OrderId, Code, checkedInAt, checkedInAt));
    }

    public Ticket Transfer(
        Contact recipient,
        TicketCode replacementCode,
        DateTimeOffset transferredAt,
        TicketId? replacementTicketId = null)
    {
        if (Status == TicketStatus.CheckedIn)
        {
            throw new BusinessRuleValidationException(
                "TICKET_ALREADY_CHECKED_IN",
                "A checked-in ticket cannot be transferred.");
        }

        if (Status != TicketStatus.Valid)
        {
            throw new BusinessRuleValidationException(
                "TICKET_NOT_VALID_FOR_TRANSFER",
                "This ticket is not valid for transfer.");
        }

        Status = TicketStatus.Transferred;

        var replacementTicket = Issue(
            EventId,
            OrderId,
            TicketTypeId,
            recipient,
            replacementCode,
            transferredAt,
            replacementTicketId);

        Raise(new TicketTransferredEvent(Id, replacementTicket.Id, EventId, OrderId, Code, replacementCode, transferredAt));

        return replacementTicket;
    }

    public static Ticket FromPersistence(
        TicketId id,
        EventId eventId,
        OrderId orderId,
        TicketTypeId ticketTypeId,
        TicketCode code,
        Contact holder,
        TicketStatus status,
        DateTimeOffset issuedAt,
        DateTimeOffset? checkedInAt,
        DateTimeOffset? lastDeliveredAt,
        long rowVersion) =>
        new()
        {
            Id = id,
            EventId = eventId,
            OrderId = orderId,
            TicketTypeId = ticketTypeId,
            Code = code,
            Holder = holder,
            Status = status,
            IssuedAt = issuedAt,
            CheckedInAt = checkedInAt,
            LastDeliveredAt = lastDeliveredAt,
            RowVersion = rowVersion,
        };
}
