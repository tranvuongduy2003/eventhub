namespace EventHub.Contracts.Events;

public sealed record PublicEventListItemResponse(
    string Slug,
    string Title,
    DateTimeOffset? StartsAt,
    string? TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline,
    string? CoverImageUrl,
    decimal? LowestPriceAmount,
    string? LowestPriceCurrency,
    bool IsSoldOut);
