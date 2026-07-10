namespace EventHub.Contracts.Reporting;

public sealed record EventAttendeeResponse(
    string Name,
    string Email,
    int TicketTypeId,
    string TicketTypeName,
    int OrderId,
    int TicketId,
    bool CheckedIn,
    DateTimeOffset? CheckedInAt);

public sealed record EventAttendeeListResponse(IReadOnlyList<EventAttendeeResponse> Attendees);
