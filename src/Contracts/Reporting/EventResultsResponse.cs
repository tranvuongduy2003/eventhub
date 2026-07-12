namespace EventHub.Contracts.Reporting;

public sealed record EventResultsResponse(
    int EventId,
    string EventTitle,
    decimal TotalRevenueAmount,
    string TotalRevenueCurrency,
    int IssuedCount,
    int CheckedInCount,
    int NoShowCount,
    decimal CheckInRate,
    IReadOnlyList<TicketTypeSalesResponse> TicketsSoldByType);

public sealed record TicketTypeSalesResponse(
    int TicketTypeId,
    string TicketTypeName,
    int Capacity,
    int SoldCount,
    int ReservedCount,
    int RemainingCount,
    decimal RevenueAmount,
    string RevenueCurrency);
