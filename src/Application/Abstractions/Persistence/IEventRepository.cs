using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IEventRepository
{
    Task AddAsync(Event domain, CancellationToken cancellationToken = default);

    Task<Event?> GetByIdAsync(EventId eventId, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task Update(Event domain, CancellationToken cancellationToken = default);
}
