using EventHub.Application.Abstractions.Messaging;
using EventHub.Contracts.Events;

namespace EventHub.Application.Events.Queries;

public sealed record GetEventOccurrencesQuery(int EventId)
    : IQuery<IReadOnlyList<OccurrenceResponse>>;
