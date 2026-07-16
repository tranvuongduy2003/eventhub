namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class CheckInReplayRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public required string ClientScanId { get; set; }

    public required string CodeFingerprint { get; set; }

    public DateTimeOffset ScannedAtUtc { get; set; }

    public bool Accepted { get; set; }

    public required string ResponseStatus { get; set; }

    public string? RejectionReason { get; set; }

    public int? TicketId { get; set; }

    public DateTimeOffset? CheckedInAt { get; set; }

    public DateTimeOffset ResolvedAt { get; set; }
}
