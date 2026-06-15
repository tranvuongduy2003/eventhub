namespace EventHub.Contracts.Users;

public sealed record LoginUserRequest(string Email, string Password);
