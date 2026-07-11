namespace EventHub.Contracts.Tickets;

public sealed record TransferTicketRequest(string RecipientName, string RecipientEmail);
