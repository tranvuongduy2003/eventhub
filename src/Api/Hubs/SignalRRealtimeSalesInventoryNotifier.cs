using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Realtime;
using Microsoft.AspNetCore.SignalR;
using DomainEventId = EventHub.Domain.Events.EventId;

namespace EventHub.Api.Hubs;

internal sealed class SignalRRealtimeSalesInventoryNotifier(
    IHubContext<EventMonitoringHub> hubContext,
    IReportingRepository reportingRepository,
    IClock clock,
    ILogger<SignalRRealtimeSalesInventoryNotifier> logger)
    : IRealtimeSalesInventoryNotifier
{
    public async Task NotifySalesInventoryChangedAsync(
        DomainEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var results = await reportingRepository.GetEventResultsAsync(eventId, cancellationToken);
        var message = new EventSalesInventoryUpdatedMessage(
            results.EventId,
            results.EventTitle,
            results.TotalRevenueAmount,
            results.TotalRevenueCurrency,
            results.IssuedCount,
            results.TicketsSoldByType.Select(ticketType => new TicketTypeSalesInventoryMessage(
                ticketType.TicketTypeId,
                ticketType.TicketTypeName,
                ticketType.Capacity,
                ticketType.SoldCount,
                ticketType.ReservedCount,
                ticketType.RemainingCount,
                ticketType.RevenueAmount,
                ticketType.RevenueCurrency)).ToList(),
            clock.UtcNow);

        logger.LogInformation(
            "Realtime sales inventory broadcast attempt for event {EventId} with {TicketTypeCount} ticket types",
            eventId.Value,
            message.TicketTypes.Count);

        await hubContext.Clients
            .Group(EventMonitoringGroups.SalesInventory(eventId.Value))
            .SendAsync("eventSalesInventoryUpdated", message, cancellationToken);
    }
}

internal sealed record EventSalesInventoryUpdatedMessage(
    int EventId,
    string EventTitle,
    decimal TotalRevenueAmount,
    string TotalRevenueCurrency,
    int IssuedCount,
    IReadOnlyList<TicketTypeSalesInventoryMessage> TicketTypes,
    DateTimeOffset OccurredAt);

internal sealed record TicketTypeSalesInventoryMessage(
    int TicketTypeId,
    string TicketTypeName,
    int Capacity,
    int SoldCount,
    int ReservedCount,
    int RemainingCount,
    decimal RevenueAmount,
    string RevenueCurrency);
