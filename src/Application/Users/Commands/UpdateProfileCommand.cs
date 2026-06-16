using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Commands;

public sealed record UpdateProfileCommand(
    string? DisplayName,
    string? Email) : ICommand<UpdateProfileResult>;

public sealed record UpdateProfileResult(
    Guid UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl);
