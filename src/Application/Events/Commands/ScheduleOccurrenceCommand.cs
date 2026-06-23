using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record ScheduleOccurrenceCommand(
    int EventId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address)
    : ICommand<ScheduleOccurrenceResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.EventManagement;
}

public sealed record ScheduleOccurrenceResult(
    int OccurrenceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? VenueName,
    string? Address,
    DateTimeOffset CreatedAt);
