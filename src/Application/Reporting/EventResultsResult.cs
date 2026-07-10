namespace EventHub.Application.Reporting;

public sealed record EventResultsResult(
    int EventId,
    string EventTitle,
    decimal TotalRevenueAmount,
    string TotalRevenueCurrency,
    int IssuedCount,
    int CheckedInCount,
    int NoShowCount,
    decimal CheckInRate,
    IReadOnlyList<TicketTypeSalesResult> TicketsSoldByType);

public sealed record TicketTypeSalesResult(
    int TicketTypeId,
    string TicketTypeName,
    int SoldCount,
    decimal RevenueAmount,
    string RevenueCurrency);
