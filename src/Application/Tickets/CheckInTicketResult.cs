namespace EventHub.Application.Tickets;

public sealed record CheckInTicketResult(
    int TicketId,
    int EventId,
    int OrderId,
    int TicketTypeId,
    string Code,
    string HolderName,
    string HolderEmail,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? CheckedInAt);
