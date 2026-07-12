namespace EventHub.Contracts.Tickets;

public sealed record BatchCheckInTicketsResponse(IReadOnlyList<BatchCheckInTicketResponse> Results);

public sealed record BatchCheckInTicketResponse(
    string ClientScanId,
    string Code,
    bool Accepted,
    string Status,
    string? Reason,
    CheckInTicketResponse? Ticket);
