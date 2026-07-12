using System.Security.Claims;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using Microsoft.AspNetCore.SignalR;
using DomainEventId = EventHub.Domain.Events.EventId;

namespace EventHub.Api.Hubs;

public sealed class EventMonitoringHub(
    IPermissionCache permissionCache,
    IEventRepository eventRepository,
    ILogger<EventMonitoringHub> logger)
    : Hub
{
    private const string SalesInventoryEventsItemKey = "EventMonitoringHub.SalesInventoryEvents";
    private const string CheckInEventsItemKey = "EventMonitoringHub.CheckInEvents";

    public async Task JoinEventSalesInventory(int eventId)
    {
        await EnsureReportingPermissionAsync(eventId, Context.ConnectionAborted);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            EventMonitoringGroups.SalesInventory(eventId),
            Context.ConnectionAborted);

        GetJoinedSalesInventoryEvents().Add(eventId);
        logger.LogInformation(
            "Realtime sales inventory join accepted for event {EventId}",
            eventId);
    }

    public async Task LeaveEventSalesInventory(int eventId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            EventMonitoringGroups.SalesInventory(eventId),
            Context.ConnectionAborted);

        GetJoinedSalesInventoryEvents().Remove(eventId);
        logger.LogInformation(
            "Realtime sales inventory leave completed for event {EventId}",
            eventId);
    }

    public async Task JoinEventCheckIn(int eventId)
    {
        await EnsureCheckInPermissionAsync(eventId, Context.ConnectionAborted);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            EventMonitoringGroups.CheckIn(eventId),
            Context.ConnectionAborted);

        GetJoinedCheckInEvents().Add(eventId);
        logger.LogInformation(
            "Realtime check-in join accepted for event {EventId}",
            eventId);
    }

    public async Task LeaveEventCheckIn(int eventId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            EventMonitoringGroups.CheckIn(eventId),
            Context.ConnectionAborted);

        GetJoinedCheckInEvents().Remove(eventId);
        logger.LogInformation(
            "Realtime check-in leave completed for event {EventId}",
            eventId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var joinedEventsCount = GetJoinedSalesInventoryEvents().Count;
        var joinedCheckInEventsCount = GetJoinedCheckInEvents().Count;
        logger.LogInformation(
            "Realtime connection disconnected; cleaned up {JoinedEventsCount} sales groups and {JoinedCheckInEventsCount} check-in groups",
            joinedEventsCount,
            joinedCheckInEventsCount);

        return base.OnDisconnectedAsync(exception);
    }

    private async Task EnsureCheckInPermissionAsync(int eventIdValue, CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(eventIdValue, Permission.CheckIn, "check-in");
    }

    private async Task EnsureReportingPermissionAsync(int eventIdValue, CancellationToken cancellationToken)
    {
        await EnsurePermissionAsync(eventIdValue, Permission.Reporting, "sales inventory");
    }

    private async Task EnsurePermissionAsync(int eventIdValue, Permission permission, string area)
    {
        if (GetCurrentUserId() is not { } userId)
        {
            logger.LogWarning(
                "Realtime {Area} join refused for event {EventId}: unauthenticated connection",
                area,
                eventIdValue);

            throw new HubException("You must be logged in.");
        }

        var eventId = DomainEventId.From(eventIdValue);
        var role = await permissionCache.GetRoleAsync(eventId, userId, Context.ConnectionAborted);

        if (role is null)
        {
            var eventAggregate = await eventRepository.GetByIdAsync(eventId, Context.ConnectionAborted);
            if (eventAggregate?.OrganizerId == userId)
            {
                role = EventRole.Owner;
            }
        }

        if (role is null || !EventRolePermissions.GetPermissions(role.Value).Contains(permission))
        {
            logger.LogWarning(
                "Realtime {Area} join refused for event {EventId}: permission missing",
                area,
                eventIdValue);

            throw new HubException("You do not have the required permissions to monitor this event.");
        }
    }

    private HashSet<int> GetJoinedSalesInventoryEvents()
    {
        if (Context.Items.TryGetValue(SalesInventoryEventsItemKey, out var value) &&
            value is HashSet<int> joinedEvents)
        {
            return joinedEvents;
        }

        joinedEvents = [];
        Context.Items[SalesInventoryEventsItemKey] = joinedEvents;

        return joinedEvents;
    }

    private HashSet<int> GetJoinedCheckInEvents()
    {
        if (Context.Items.TryGetValue(CheckInEventsItemKey, out var value) &&
            value is HashSet<int> joinedEvents)
        {
            return joinedEvents;
        }

        joinedEvents = [];
        Context.Items[CheckInEventsItemKey] = joinedEvents;

        return joinedEvents;
    }

    private UserId? GetCurrentUserId()
    {
        var userIdValue = Context.User?.FindFirstValue(SessionAuthenticationDefaults.UserIdClaimType);

        return Guid.TryParse(userIdValue, out var parsedUserId)
            ? UserId.From(parsedUserId)
            : null;
    }
}

internal static class EventMonitoringGroups
{
    public static string SalesInventory(int eventId) => $"event:{eventId}:sales";

    public static string CheckIn(int eventId) => $"event:{eventId}:check-in";
}
