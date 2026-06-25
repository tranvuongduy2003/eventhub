using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Services;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.BackgroundJobs;

internal sealed class ReservationHoldExpiryJob(
    IServiceProvider serviceProvider,
    ILogger<ReservationHoldExpiryJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Reservation hold expiry job started.");

        using var timer = new PeriodicTimer(PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await ProcessExpiredReservations(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error processing expired reservations.");
            }
        }

        logger.LogInformation("Reservation hold expiry job stopped.");
    }

    private async Task ProcessExpiredReservations(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var pendingDomainEventsCollector = scope.ServiceProvider.GetRequiredService<IPendingDomainEventsCollector>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;

        // Find expired reservations where the associated order is still Pending
        var expiredRecords = await dbContext.Reservations
            .Where(r => r.ExpiresAt < now)
            .Join(
                dbContext.Orders.Where(o => o.Status == "Pending"),
                r => r.OrderId,
                o => o.Id,
                (r, o) => new { Reservation = r, OrderId = o.Id })
            .ToListAsync(cancellationToken);

        if (expiredRecords.Count == 0)
        {
            return;
        }

        logger.LogInformation("Found {Count} expired reservations to process.", expiredRecords.Count);

        // Group by event to minimize Event aggregate loads
        var byEvent = expiredRecords.GroupBy(x => x.Reservation.EventId);

        foreach (var eventGroup in byEvent)
        {
            var eventId = eventGroup.Key;
            var retries = 0;

            while (retries < MaxRetries)
            {
                try
                {
                    var eventRecord = await dbContext.Events
                        .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

                    if (eventRecord is null)
                    {
                        logger.LogWarning("Event {EventId} not found for expired reservation.", eventId);
                        break;
                    }

                    var eventAggregate = EventPersistenceMapper.ToDomain(eventRecord);

                    var occurrenceRecords = await dbContext.Occurrences
                        .Where(o => o.EventId == eventId)
                        .OrderBy(o => o.StartsAt)
                        .ToListAsync(cancellationToken);
                    eventAggregate.LoadOccurrences(occurrenceRecords.Select(OccurrencePersistenceMapper.ToDomain).ToList());

                    var ticketTypeRecords = await dbContext.TicketTypes
                        .Where(t => t.EventId == eventId)
                        .OrderBy(t => t.Id)
                        .ToListAsync(cancellationToken);
                    eventAggregate.LoadTicketTypes(ticketTypeRecords.Select(TicketTypePersistenceMapper.ToDomain).ToList());

                    var reservationRecords = await dbContext.Reservations
                        .Where(r => r.EventId == eventId)
                        .OrderBy(r => r.Id)
                        .ToListAsync(cancellationToken);
                    eventAggregate.LoadReservations(reservationRecords.Select(ReservationPersistenceMapper.ToDomain).ToList());

                    foreach (var item in eventGroup)
                    {
                        var reservationId = ReservationId.From(item.Reservation.Id);

                        // Load and expire the order
                        var orderRecord = await dbContext.Orders
                            .Include(o => o.Lines)
                            .FirstOrDefaultAsync(o => o.Id == item.OrderId, cancellationToken);

                        if (orderRecord is not null)
                        {
                            var order = OrderPersistenceMapper.ToDomain(orderRecord);
                            order.Expire(now);
                            order.ClearDomainEvents();

                            // Release reservation on the event aggregate
                            eventAggregate.ReleaseReservation(reservationId, now);

                            // Update order record
                            orderRecord.Status = order.Status.ToString();
                            orderRecord.ExpiresAt = order.ExpiresAt;
                        }
                    }

                    // Sync event aggregate (reservations, ticket types)
                    // We need to use the Update method on EventRepository, but we have DbContext directly.
                    // Let's manually sync the changes.
                    var eventIdValue = eventId;

                    // Sync ticket types
                    var existingTicketTypeRecords = await dbContext.TicketTypes
                        .Where(t => t.EventId == eventIdValue)
                        .ToListAsync(cancellationToken);

                    foreach (var ticketType in eventAggregate.TicketTypes)
                    {
                        var existing = existingTicketTypeRecords
                            .FirstOrDefault(r => r.Id == ticketType.Id.Value);
                        if (existing is not null)
                        {
                            existing.Sold = ticketType.Sold;
                            existing.Reserved = ticketType.Reserved;
                            existing.UpdatedAt = ticketType.UpdatedAt;
                        }
                    }

                    // Sync reservations (remove committed/released ones)
                    var existingReservationRecords = await dbContext.Reservations
                        .Where(r => r.EventId == eventIdValue)
                        .ToListAsync(cancellationToken);

                    var domainReservationIds = eventAggregate.Reservations.Select(r => r.Id.Value).ToHashSet();
                    var reservationsToRemove = existingReservationRecords
                        .Where(r => !domainReservationIds.Contains(r.Id))
                        .ToList();
                    dbContext.Reservations.RemoveRange(reservationsToRemove);

                    // Update event timestamp
                    eventRecord.UpdatedAt = now;

                    await dbContext.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Processed {Count} expired reservations for event {EventId}.",
                        eventGroup.Count(),
                        eventId);

                    break; // Success
                }
                catch (DbUpdateConcurrencyException)
                {
                    retries++;
                    logger.LogWarning(
                        "Concurrency conflict processing expired reservations for event {EventId}. Retry {Retry}/{MaxRetries}.",
                        eventId,
                        retries,
                        MaxRetries);

                    if (retries >= MaxRetries)
                    {
                        logger.LogError(
                            "Max retries exceeded for expired reservations on event {EventId}.",
                            eventId);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Error processing expired reservations for event {EventId}.",
                        eventId);
                    break;
                }
            }
        }
    }
}
