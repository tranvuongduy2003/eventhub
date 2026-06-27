using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;

namespace EventHub.Application.Events.Queries;

public sealed class ListEventLocationsQueryHandler(
    IEventRepository eventRepository,
    IClock clock)
    : QueryHandler<ListEventLocationsQuery, List<string>>
{
    public override async Task<Result<List<string>>> Handle(
        ListEventLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var locations = await eventRepository.GetDistinctLocationsAsync(now, cancellationToken);

        return Result<List<string>>.Success(locations);
    }
}
