using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Commands;

public sealed record RegisterUserCommand(
    string DisplayName,
    string Email,
    string Password) : ICommand<RegisterUserResult>;

public sealed record RegisterUserResult(
    Guid UserId,
    string DisplayName,
    string Email,
    DateTimeOffset CreatedAt,
    Guid SessionId,
    DateTimeOffset SessionExpiresAt);
