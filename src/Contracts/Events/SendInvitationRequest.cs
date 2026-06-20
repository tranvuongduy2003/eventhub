namespace EventHub.Contracts.Events;

public sealed record SendInvitationRequest(
    string Email,
    int? ExpiresInDays);
