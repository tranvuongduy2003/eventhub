using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Domain.Tickets;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class TicketPersistenceMapper
{
    public static TicketRecord ToRecord(Ticket ticket) =>
        new()
        {
            Id = ticket.Id.Value,
            EventId = ticket.EventId.Value,
            OrderId = ticket.OrderId.Value,
            TicketTypeId = ticket.TicketTypeId.Value,
            Code = ticket.Code.Value,
            HolderName = ticket.Holder.Name,
            HolderEmail = ticket.Holder.Email,
            Status = ticket.Status.ToString(),
            IssuedAt = ticket.IssuedAt,
            CheckedInAt = ticket.CheckedInAt,
            LastDeliveredAt = ticket.LastDeliveredAt,
            RowVersion = ticket.RowVersion,
        };

    public static Ticket ToDomain(TicketRecord record) =>
        Ticket.FromPersistence(
            TicketId.From(record.Id),
            EventId.From(record.EventId),
            OrderId.From(record.OrderId),
            TicketTypeId.From(record.TicketTypeId),
            TicketCode.Create(record.Code),
            Contact.Create(record.HolderName, record.HolderEmail),
            Enum.Parse<TicketStatus>(record.Status),
            record.IssuedAt,
            record.CheckedInAt,
            record.LastDeliveredAt,
            record.RowVersion);
}
