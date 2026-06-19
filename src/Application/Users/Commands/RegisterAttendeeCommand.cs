using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Commands;

public sealed record RegisterAttendeeCommand(
    string DisplayName,
    string Email,
    string Password) : ICommand<RegisterAttendeeResult>;

public sealed record RegisterAttendeeResult(
    Guid UserId,
    string DisplayName,
    string Email,
    string Role,
    DateTimeOffset CreatedAt,
    Guid SessionId,
    DateTimeOffset SessionExpiresAt);
