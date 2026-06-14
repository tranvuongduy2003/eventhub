using Solution.Application.Abstractions.Messaging;

namespace Solution.Application.Users.Commands;

public sealed record RegisterUserCommand(
    string Username,
    string Email,
    string Password) : ICommand<RegisterUserResult>;

public sealed record RegisterUserResult(
    Guid UserId,
    string Username,
    string Email,
    DateTimeOffset CreatedAt,
    Guid SessionId,
    DateTimeOffset SessionExpiresAt);
