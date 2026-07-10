namespace EventHub.Contracts.Reporting;

public sealed record OrganizerAudienceOverviewResponse(
    IReadOnlyList<OwnedEventOverviewResponse> OwnedEvents,
    IReadOnlyList<StaffEventOverviewResponse> StaffEvents);

public sealed record OwnedEventOverviewResponse(
    int EventId,
    string Title,
    string Status,
    DateTimeOffset? StartsAt,
    string? TimeZoneId,
    int SoldCount,
    decimal TotalRevenueAmount,
    string TotalRevenueCurrency,
    int CheckedInCount,
    int IssuedCount);

public sealed record StaffEventOverviewResponse(
    int EventId,
    string Title,
    string Status,
    DateTimeOffset? StartsAt,
    string? TimeZoneId,
    int CheckedInCount,
    int IssuedCount);
