using Solution.Application.Abstractions.Messaging;

namespace Solution.Application.Users.Commands;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<LoginUserResult>;

public sealed record LoginUserResult(
    Guid UserId,
    string Username,
    string Email,
    Guid SessionId,
    DateTimeOffset SessionExpiresAt);
