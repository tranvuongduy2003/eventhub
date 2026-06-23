using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record OccurrenceUpdatedEvent(
    EventId EventId,
    OccurrenceId OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : DomainEvent;
