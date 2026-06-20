namespace EventHub.Contracts.Events;

public sealed record InvitationResponse(
    int Id,
    string Email,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RevokedAt,
    string? Token = null);
