using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record OccurrenceScheduledEvent(
    EventId EventId,
    OccurrenceId OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : DomainEvent;
