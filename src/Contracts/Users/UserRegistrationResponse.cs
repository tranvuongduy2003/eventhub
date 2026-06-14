namespace Solution.Contracts.Users;

public sealed record UserRegistrationResponse(
    Guid UserId,
    string Username,
    string Email,
    DateTimeOffset CreatedAt);
