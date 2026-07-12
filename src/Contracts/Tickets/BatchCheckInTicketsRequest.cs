namespace EventHub.Contracts.Tickets;

public sealed record BatchCheckInTicketsRequest(IReadOnlyList<BatchCheckInTicketRequest> Tickets);

public sealed record BatchCheckInTicketRequest(
    string ClientScanId,
    string Code,
    DateTimeOffset ScannedAt);
