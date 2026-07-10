namespace EventHub.Application.Tickets;

public sealed record TicketResult(
    int TicketId,
    int EventId,
    string EventTitle,
    DateTimeOffset EventStartsAt,
    DateTimeOffset EventEndsAt,
    string EventTimeZoneId,
    string? EventLocation,
    bool EventIsOnline,
    int OrderId,
    int TicketTypeId,
    string TicketTypeName,
    string Code,
    string HolderName,
    string HolderEmail,
    string Status,
    DateTimeOffset IssuedAt);
