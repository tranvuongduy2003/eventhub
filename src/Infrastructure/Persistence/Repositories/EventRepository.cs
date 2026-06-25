using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class EventRepository(ApplicationDatabaseContext databaseContext) : IEventRepository
{
    public async Task AddAsync(Event domain, CancellationToken cancellationToken = default)
    {
        var record = EventPersistenceMapper.ToRecord(domain);
        await databaseContext.Events.AddAsync(record, cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(EventId eventId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId.Value, cancellationToken);

        if (record is null)
        {
            return null;
        }

        var occurrenceRecords = await databaseContext.Occurrences
            .AsNoTracking()
            .Where(o => o.EventId == eventId.Value)
            .OrderBy(o => o.StartsAt)
            .ToListAsync(cancellationToken);

        var ticketTypeRecords = await databaseContext.TicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == eventId.Value)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var reservationRecords = await databaseContext.Reservations
            .AsNoTracking()
            .Where(r => r.EventId == eventId.Value)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var domainEvent = EventPersistenceMapper.ToDomain(record);
        var occurrences = occurrenceRecords.Select(OccurrencePersistenceMapper.ToDomain).ToList();
        domainEvent.LoadOccurrences(occurrences);

        var ticketTypes = ticketTypeRecords.Select(TicketTypePersistenceMapper.ToDomain).ToList();
        domainEvent.LoadTicketTypes(ticketTypes);

        var reservations = reservationRecords.Select(ReservationPersistenceMapper.ToDomain).ToList();
        domainEvent.LoadReservations(reservations);

        return domainEvent;
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        await databaseContext.Events.AnyAsync(e => e.Slug == slug, cancellationToken);

    public async Task Update(Event domain, CancellationToken cancellationToken = default)
    {
        var record = EventPersistenceMapper.ToRecord(domain);
        databaseContext.Events.Update(record);

        // Sync occurrences with the domain aggregate
        var eventIdValue = domain.Id.Value;

        var existingRecords = await databaseContext.Occurrences
            .Where(o => o.EventId == eventIdValue)
            .ToListAsync(cancellationToken);

        var domainIds = domain.Occurrences.Select(o => o.Id.Value).ToHashSet();

        // Remove occurrences that no longer exist in the domain
        var toRemove = existingRecords.Where(r => !domainIds.Contains(r.Id)).ToList();
        databaseContext.Occurrences.RemoveRange(toRemove);

        // Update existing or add new occurrences
        foreach (var occurrence in domain.Occurrences)
        {
            var existing = existingRecords.FirstOrDefault(r => r.Id == occurrence.Id.Value);
            if (existing is not null)
            {
                existing.StartsAt = occurrence.StartsAt;
                existing.EndsAt = occurrence.EndsAt;
                existing.VenueName = occurrence.VenueName;
                existing.Address = occurrence.Address;
                existing.UpdatedAt = occurrence.UpdatedAt;
            }
            else
            {
                var newRecord = OccurrencePersistenceMapper.ToRecord(occurrence, eventIdValue);
                await databaseContext.Occurrences.AddAsync(newRecord, cancellationToken);
            }
        }

        // Sync ticket types with the domain aggregate
        var existingTicketTypeRecords = await databaseContext.TicketTypes
            .Where(t => t.EventId == eventIdValue)
            .ToListAsync(cancellationToken);

        var domainTicketTypeIds = domain.TicketTypes.Select(t => t.Id.Value).ToHashSet();

        // Remove ticket types that no longer exist in the domain
        var ticketTypesToRemove = existingTicketTypeRecords
            .Where(r => !domainTicketTypeIds.Contains(r.Id))
            .ToList();
        databaseContext.TicketTypes.RemoveRange(ticketTypesToRemove);

        // Update existing or add new ticket types
        foreach (var ticketType in domain.TicketTypes)
        {
            var existing = existingTicketTypeRecords
                .FirstOrDefault(r => r.Id == ticketType.Id.Value);
            if (existing is not null)
            {
                existing.Name = ticketType.Name.Value;
                existing.PriceAmount = ticketType.Price.Amount;
                existing.PriceCurrency = ticketType.Price.Currency;
                existing.Capacity = ticketType.Capacity.Value;
                existing.Sold = ticketType.Sold;
                existing.Reserved = ticketType.Reserved;
                existing.UpdatedAt = ticketType.UpdatedAt;
            }
            else
            {
                var newRecord = TicketTypePersistenceMapper.ToRecord(ticketType, eventIdValue);
                await databaseContext.TicketTypes.AddAsync(newRecord, cancellationToken);
            }
        }

        // Sync reservations with the domain aggregate
        var existingReservationRecords = await databaseContext.Reservations
            .Where(r => r.EventId == eventIdValue)
            .ToListAsync(cancellationToken);

        var domainReservationIds = domain.Reservations.Select(r => r.Id.Value).ToHashSet();

        // Remove reservations that no longer exist in the domain (committed/released)
        var reservationsToRemove = existingReservationRecords
            .Where(r => !domainReservationIds.Contains(r.Id))
            .ToList();
        databaseContext.Reservations.RemoveRange(reservationsToRemove);

        // Update existing or add new reservations
        foreach (var reservation in domain.Reservations)
        {
            var existing = existingReservationRecords
                .FirstOrDefault(r => r.Id == reservation.Id.Value);
            if (existing is not null)
            {
                existing.TicketTypeId = reservation.TicketTypeId.Value;
                existing.Quantity = reservation.Quantity;
                existing.OrderId = reservation.OrderId.Value;
                existing.ExpiresAt = reservation.ExpiresAt;
            }
            else
            {
                var newRecord = ReservationPersistenceMapper.ToRecord(reservation, eventIdValue);
                await databaseContext.Reservations.AddAsync(newRecord, cancellationToken);
            }
        }
    }
}
