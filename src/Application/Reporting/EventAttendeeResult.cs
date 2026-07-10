namespace EventHub.Application.Reporting;

public sealed record EventAttendeeResult(
    string Name,
    string Email,
    int TicketTypeId,
    string TicketTypeName,
    int OrderId,
    int TicketId,
    bool CheckedIn,
    DateTimeOffset? CheckedInAt);
