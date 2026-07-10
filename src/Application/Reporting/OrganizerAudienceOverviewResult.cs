namespace EventHub.Application.Reporting;

public sealed record OrganizerAudienceOverviewResult(
    IReadOnlyList<OwnedEventOverviewResult> OwnedEvents,
    IReadOnlyList<StaffEventOverviewResult> StaffEvents);

public sealed record OwnedEventOverviewResult(
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

public sealed record StaffEventOverviewResult(
    int EventId,
    string Title,
    string Status,
    DateTimeOffset? StartsAt,
    string? TimeZoneId,
    int CheckedInCount,
    int IssuedCount);
