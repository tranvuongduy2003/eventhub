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

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var joinedEventsCount = GetJoinedSalesInventoryEvents().Count;
        logger.LogInformation(
            "Realtime sales inventory connection disconnected; cleaned up {JoinedEventsCount} joined event groups",
            joinedEventsCount);

        return base.OnDisconnectedAsync(exception);
    }

    private async Task EnsureReportingPermissionAsync(int eventIdValue, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId)
        {
            logger.LogWarning(
                "Realtime sales inventory join refused for event {EventId}: unauthenticated connection",
                eventIdValue);

            throw new HubException("You must be logged in.");
        }

        var eventId = DomainEventId.From(eventIdValue);
        var role = await permissionCache.GetRoleAsync(eventId, userId, cancellationToken);

        if (role is null)
        {
            var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (eventAggregate?.OrganizerId == userId)
            {
                role = EventRole.Owner;
            }
        }

        if (role is null || !EventRolePermissions.GetPermissions(role.Value).Contains(Permission.Reporting))
        {
            logger.LogWarning(
                "Realtime sales inventory join refused for event {EventId}: reporting permission missing",
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
}
