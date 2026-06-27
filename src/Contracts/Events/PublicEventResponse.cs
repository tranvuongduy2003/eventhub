namespace EventHub.Contracts.Events;

public sealed record PublicEventResponse(
    string Slug,
    string Title,
    string? Description,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline,
    string? CoverImageUrl,
    string Status,
    bool Purchasable,
    List<PublicTicketTypeResponse> TicketTypes);
