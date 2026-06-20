using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using MediatR;

namespace EventHub.Application.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserAccessor currentUserAccessor,
    IPermissionCache permissionCache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuthorizeEventOperation
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return ResultFactory.CreateFailure<TResponse>(
                Error.Unauthorized("UNAUTHORIZED", "You must be logged in."));
        }

        var eventId = request.EventId;
        var requiredPermission = request.RequiredPermission;

        var role = await permissionCache.GetRoleAsync(eventId, userId, cancellationToken);

        if (role is null)
        {
            return ResultFactory.CreateFailure<TResponse>(
                Error.Forbidden(
                    "INSUFFICIENT_PERMISSIONS",
                    "You do not have the required permissions to perform this operation on this event."));
        }

        var permissions = EventRolePermissions.GetPermissions(role.Value);

        if (!permissions.Contains(requiredPermission))
        {
            return ResultFactory.CreateFailure<TResponse>(
                Error.Forbidden(
                    "INSUFFICIENT_PERMISSIONS",
                    "You do not have the required permissions to perform this operation on this event."));
        }

        return await next(cancellationToken);
    }
}
