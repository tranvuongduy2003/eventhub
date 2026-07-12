using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Realtime;
using Microsoft.AspNetCore.SignalR;
using DomainEventId = EventHub.Domain.Events.EventId;

namespace EventHub.Api.Hubs;

internal sealed class SignalRRealtimeCheckInNotifier(
    IHubContext<EventMonitoringHub> hubContext,
    ITicketRepository ticketRepository,
    IClock clock,
    ILogger<SignalRRealtimeCheckInNotifier> logger)
    : IRealtimeCheckInNotifier
{
    public async Task NotifyCheckInChangedAsync(
        DomainEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var counts = await ticketRepository.GetDoorCountsAsync(eventId, cancellationToken);
        var message = new EventCheckInUpdatedMessage(
            eventId.Value,
            counts.CheckedIn,
            counts.TotalIssued,
            counts.TotalIssued == 0 ? 0 : (decimal)counts.CheckedIn / counts.TotalIssued,
            clock.UtcNow);

        logger.LogInformation(
            "Realtime check-in broadcast attempt for event {EventId}",
            eventId.Value);

        await hubContext.Clients
            .Group(EventMonitoringGroups.CheckIn(eventId.Value))
            .SendAsync("eventCheckInUpdated", message, cancellationToken);
    }
}

internal sealed record EventCheckInUpdatedMessage(
    int EventId,
    int CheckedIn,
    int TotalIssued,
    decimal CheckInRate,
    DateTimeOffset OccurredAt);
