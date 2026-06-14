namespace Solution.Contracts.Users;

public sealed record LoginUserResponse(Guid UserId, string Username, string Email);
