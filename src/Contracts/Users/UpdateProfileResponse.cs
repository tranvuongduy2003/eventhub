namespace EventHub.Contracts.Users;

public sealed record UpdateProfileResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl);
