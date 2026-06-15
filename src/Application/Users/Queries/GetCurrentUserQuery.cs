using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Users.Queries;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResult>;

public sealed record CurrentUserResult(Guid UserId, string DisplayName, string Email);
