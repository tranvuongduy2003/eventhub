namespace EventHub.Contracts.Tickets;

public sealed record DoorCountsResponse(int CheckedIn, int TotalIssued);
