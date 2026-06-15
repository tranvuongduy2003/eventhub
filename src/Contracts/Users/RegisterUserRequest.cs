namespace EventHub.Contracts.Users;

public sealed record RegisterUserRequest(string DisplayName, string Email, string Password);
