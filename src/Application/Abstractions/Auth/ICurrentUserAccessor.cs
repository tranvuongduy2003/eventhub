using EventHub.Domain.Users;

namespace EventHub.Application.Abstractions.Auth;

public interface ICurrentUserAccessor
{
    UserId? UserId { get; }

    bool IsAuthenticated { get; }
}
