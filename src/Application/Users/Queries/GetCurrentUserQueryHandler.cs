using Solution.Application.Abstractions.Auth;
using Solution.Application.Abstractions.Messaging;
using Solution.Application.Abstractions.Persistence;
using Solution.Application.Common;

namespace Solution.Application.Users.Queries;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository)
    : QueryHandler<GetCurrentUserQuery, CurrentUserResult>
{
    public override async Task<Result<CurrentUserResult>> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is null)
        {
            return Error.Unauthorized("UNAUTHORIZED", "Authentication is required.");
        }

        var user = await userRepository.GetByIdAsync(currentUserAccessor.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User was not found.");
        }

        return new CurrentUserResult(
            user.Id.Value,
            user.Username.Value,
            user.Email.DisplayValue);
    }
}
