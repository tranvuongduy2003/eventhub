namespace EventHub.Contracts.Events;

public sealed record PublicEventResponse(
    int EventId,
    string Title,
    string? Description,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline,
    List<PublicTicketTypeResponse> TicketTypes);
