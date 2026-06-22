using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record EventPublishedEvent(EventId EventId, Slug Slug) : DomainEvent;
