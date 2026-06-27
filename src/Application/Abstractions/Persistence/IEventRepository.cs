using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IEventRepository
{
    Task AddAsync(Event domain, CancellationToken cancellationToken = default);

    Task<Event?> GetByIdAsync(EventId eventId, CancellationToken cancellationToken = default);

    Task<Event?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<PaginatedResult<Event>> GetPublishedUpcomingAsync(
        int page,
        int pageSize,
        DateTimeOffset now,
        EventFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetDistinctLocationsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task Update(Event domain, CancellationToken cancellationToken = default);
}
