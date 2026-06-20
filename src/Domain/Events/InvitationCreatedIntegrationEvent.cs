using EventHub.Domain.Abstractions;

namespace EventHub.Domain.Events;

public sealed record InvitationCreatedIntegrationEvent(
    InvitationId InvitationId,
    EventId EventId,
    string Email,
    string Token,
    string EventTitle,
    DateTimeOffset ExpiresAt) : DomainEvent;
