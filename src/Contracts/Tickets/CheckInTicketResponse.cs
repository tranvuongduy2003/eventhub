namespace EventHub.Contracts.Tickets;

public sealed record CheckInTicketResponse(
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
