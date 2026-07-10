namespace EventHub.Contracts.Events;

public sealed record OrganizerEventListItemResponse(
    int EventId,
    string Title,
    string Status,
    DateTimeOffset? StartsAt,
    string? TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline,
    int TicketTypeCount,
    int SoldCount,
    DateTimeOffset UpdatedAt);
