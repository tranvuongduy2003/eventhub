using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;

namespace EventHub.Application.Users.Queries;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IObjectStorage objectStorage)
    : QueryHandler<GetCurrentUserQuery, CurrentUserResult>
{
    private const string Bucket = "eventhub";

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

        var avatarUrl = user.AvatarImageRef is not null
            ? objectStorage.GetPublicUri(Bucket, user.AvatarImageRef.Value).ToString()
            : null;

        return new CurrentUserResult(
            user.Id.Value,
            user.DisplayName.Value,
            user.Email.DisplayValue,
            user.Role.ToString(),
            avatarUrl);
    }
}
