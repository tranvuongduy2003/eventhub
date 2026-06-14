using Solution.Domain.Users;

namespace Solution.Application.Abstractions.Auth;

public interface ICurrentUserAccessor
{
    UserId? UserId { get; }

    bool IsAuthenticated { get; }
}
