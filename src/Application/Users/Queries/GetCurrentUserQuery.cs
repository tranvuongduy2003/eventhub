using Solution.Application.Abstractions.Messaging;

namespace Solution.Application.Users.Queries;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResult>;

public sealed record CurrentUserResult(Guid UserId, string Username, string Email);
