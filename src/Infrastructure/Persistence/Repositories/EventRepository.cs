using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class EventRepository(ApplicationDatabaseContext databaseContext) : IEventRepository
{
    public async Task AddAsync(Event domain, CancellationToken cancellationToken = default) =>
        await databaseContext.Events.AddAsync(EventPersistenceMapper.ToRecord(domain), cancellationToken);

    public async Task<Event?> GetByIdAsync(EventId eventId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId.Value, cancellationToken);

        return record is null ? null : EventPersistenceMapper.ToDomain(record);
    }

    public void Update(Event domain)
    {
        var record = EventPersistenceMapper.ToRecord(domain);
        databaseContext.Events.Update(record);
    }
}
