using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record RescheduleOccurrenceCommand(
    int EventId,
    int OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address)
    : ICommand<RescheduleOccurrenceResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.EventManagement;
}

public sealed record RescheduleOccurrenceResult(
    int OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address,
    DateTimeOffset UpdatedAt);
