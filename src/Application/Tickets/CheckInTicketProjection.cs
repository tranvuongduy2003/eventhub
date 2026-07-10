using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets;

internal static class CheckInTicketProjection
{
    public static CheckInTicketResult ToResult(Ticket ticket) =>
        new(
            ticket.Id.Value,
            ticket.EventId.Value,
            ticket.OrderId.Value,
            ticket.TicketTypeId.Value,
            ticket.Code.Value,
            ticket.Holder.Name,
            ticket.Holder.Email,
            ticket.Status.ToString().ToLowerInvariant(),
            ticket.IssuedAt,
            ticket.CheckedInAt);
}
