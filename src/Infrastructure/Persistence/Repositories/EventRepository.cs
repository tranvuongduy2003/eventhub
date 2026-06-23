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

        var domainEvent = EventPersistenceMapper.ToDomain(record);
        var occurrences = occurrenceRecords.Select(OccurrencePersistenceMapper.ToDomain).ToList();
        domainEvent.LoadOccurrences(occurrences);

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
    }
}
