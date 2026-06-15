using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Commands;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<LoginUserResult>;

public sealed record LoginUserResult(
    Guid UserId,
    string DisplayName,
    string Email,
    Guid SessionId,
    DateTimeOffset SessionExpiresAt);
