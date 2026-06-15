namespace EventHub.Contracts.Users;

public sealed record LoginUserResponse(Guid UserId, string DisplayName, string Email);
