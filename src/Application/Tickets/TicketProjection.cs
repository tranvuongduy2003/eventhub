using EventHub.Domain.Events;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets;

internal static class TicketProjection
{
    public static List<TicketResult> ToResults(List<Ticket> tickets, Event? eventAggregate)
    {
        var ticketTypeNameById = eventAggregate?.TicketTypes.ToDictionary(
            ticketType => ticketType.Id,
            ticketType => ticketType.Name.Value) ?? [];

        return tickets
            .OrderBy(ticket => ticket.IssuedAt)
            .ThenBy(ticket => ticket.Id.Value)
            .Select(ticket => new TicketResult(
                ticket.Id.Value,
                ticket.EventId.Value,
                eventAggregate?.Title.Value ?? $"Event {ticket.EventId.Value}",
                eventAggregate?.Schedule?.StartsAt ?? DateTimeOffset.MinValue,
                eventAggregate?.Schedule?.EndsAt ?? DateTimeOffset.MinValue,
                eventAggregate?.Schedule?.TimeZoneId ?? "UTC",
                eventAggregate?.Location.PhysicalAddress,
                eventAggregate?.Location.IsOnline ?? false,
                ticket.OrderId.Value,
                ticket.TicketTypeId.Value,
                ticketTypeNameById.TryGetValue(ticket.TicketTypeId, out var ticketTypeName)
                    ? ticketTypeName
                    : $"Ticket type {ticket.TicketTypeId.Value}",
                ticket.Code.Value,
                ticket.Holder.Name,
                ticket.Holder.Email,
                ticket.Status.ToString().ToLowerInvariant(),
                ticket.IssuedAt))
            .ToList();
    }
}
