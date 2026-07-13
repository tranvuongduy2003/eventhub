using EventHub.Domain.Events;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets;

public sealed record CheckInReplayRecord(
    EventId EventId,
    string ClientScanId,
    string CodeFingerprint,
    DateTimeOffset ScannedAtUtc,
    bool Accepted,
    string ResponseStatus,
    string? RejectionReason,
    TicketId? TicketId,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset ResolvedAt);
