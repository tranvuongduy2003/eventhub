using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Events.Commands;

public sealed record CreateDraftEventCommand(
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZoneId,
    string? PhysicalAddress,
    bool IsOnline) : ICommand<CreateDraftEventResult>;

public sealed record CreateDraftEventResult(
    int EventId,
    string Status,
    DateTimeOffset CreatedAt);
