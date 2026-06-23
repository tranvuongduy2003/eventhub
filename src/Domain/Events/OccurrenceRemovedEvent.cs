using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record OccurrenceRemovedEvent(
    EventId EventId,
    OccurrenceId OccurrenceId) : DomainEvent;
