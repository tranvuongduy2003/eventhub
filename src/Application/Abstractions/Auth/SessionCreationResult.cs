namespace Solution.Application.Abstractions.Auth;

public sealed record SessionCreationResult(Guid SessionId, DateTimeOffset ExpiresAt);
