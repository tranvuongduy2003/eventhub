using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class GetEventOccurrencesQueryHandler(
    IEventRepository eventRepository)
    : QueryHandler<GetEventOccurrencesQuery, IReadOnlyList<OccurrenceResponse>>
{
    public override async Task<Result<IReadOnlyList<OccurrenceResponse>>> Handle(
        GetEventOccurrencesQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(query.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return Error.NotFound("EVENT_NOT_FOUND", "The event was not found.");
        }

        var occurrences = eventAggregate.Occurrences
            .OrderBy(o => o.StartsAt)
            .Select(o => new OccurrenceResponse(
                o.Id.Value,
                o.StartsAt,
                o.EndsAt,
                o.VenueName,
                o.Address,
                o.CreatedAt,
                o.UpdatedAt))
            .ToList();

        return occurrences;
    }
}
