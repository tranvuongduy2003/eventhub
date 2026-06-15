using System.Security.Claims;
using EventHub.Application.Abstractions.Auth;
using EventHub.Domain.Users;

namespace EventHub.Api.Auth;

internal sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public UserId? UserId
    {
        get
        {
            var userIdValue = httpContextAccessor.HttpContext?.User
                .FindFirstValue(SessionAuthenticationDefaults.UserIdClaimType);

            return Guid.TryParse(userIdValue, out var parsedUserId)
                ? Domain.Users.UserId.From(parsedUserId)
                : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
