namespace EventHub.Contracts.Tickets;

public sealed record SearchCheckInTicketsResponse(IReadOnlyList<CheckInTicketResponse> Tickets);
