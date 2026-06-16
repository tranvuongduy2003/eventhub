namespace EventHub.Contracts.Users;

public sealed record UpdateProfileRequest(string? DisplayName, string? Email);
