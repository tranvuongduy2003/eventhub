namespace Solution.Contracts.Users;

public sealed record RegisterUserRequest(string Username, string Email, string Password);
