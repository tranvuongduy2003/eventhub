using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Users;

namespace EventHub.Application.DiscountCodes.Queries;

public sealed class GetDiscountCodesQueryHandler(
    IDiscountCodeRepository discountCodeRepository,
    IEventRepository eventRepository,
    ICurrentUserAccessor currentUserAccessor,
    IPermissionCache permissionCache,
    IClock clock)
    : QueryHandler<GetDiscountCodesQuery, List<DiscountCodeResult>>
{
    public override async Task<Result<List<DiscountCodeResult>>> Handle(
        GetDiscountCodesQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return Error.Unauthorized("UNAUTHORIZED", "You must be logged in.");
        }

        var eventId = EventId.From(query.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return DiscountCodeErrors.EventNotFound;
        }

        var role = await permissionCache.GetRoleAsync(eventId, userId, cancellationToken);
        if (role is null)
        {
            return Error.Forbidden(
                "INSUFFICIENT_PERMISSIONS",
                "You do not have the required permissions to view discount codes for this event.");
        }

        var permissions = EventRolePermissions.GetPermissions(role.Value);
        if (!permissions.Contains(Permission.Ticketing))
        {
            return Error.Forbidden(
                "INSUFFICIENT_PERMISSIONS",
                "You do not have the required permissions to view discount codes for this event.");
        }

        var discountCodes = await discountCodeRepository.GetByEventAsync(query.EventId, cancellationToken);
        var now = clock.UtcNow;

        return discountCodes
            .Where(dc => !dc.DeletedAt.HasValue)
            .Select(dc => new DiscountCodeResult(
                dc.Id.Value,
                dc.EventId,
                dc.Code,
                dc.Type.ToString(),
                dc.Value,
                dc.StartAt,
                dc.EndAt,
                dc.UsageCap,
                dc.UsedCount,
                dc.IsActive(now),
                dc.CreatedAt,
                dc.UpdatedAt))
            .ToList();
    }
}
