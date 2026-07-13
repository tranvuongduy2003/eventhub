using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using ApplicationCheckInReplayRecord = EventHub.Application.Tickets.CheckInReplayRecord;
using PersistenceCheckInReplayRecord = EventHub.Infrastructure.Persistence.Entities.CheckInReplayRecord;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class CheckInReplayRepository(ApplicationDatabaseContext databaseContext)
    : ICheckInReplayRepository
{
    public async Task AddAsync(
        ApplicationCheckInReplayRecord replayRecord,
        CancellationToken cancellationToken = default) =>
        await databaseContext.CheckInReplays.AddAsync(ToPersistenceRecord(replayRecord), cancellationToken);

    public async Task<ApplicationCheckInReplayRecord?> GetByEventAndClientScanIdAsync(
        EventId eventId,
        string clientScanId,
        CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.CheckInReplays
            .AsNoTracking()
            .SingleOrDefaultAsync(
                replay => replay.EventId == eventId.Value && replay.ClientScanId == clientScanId,
                cancellationToken);

        return record is null ? null : ToApplicationRecord(record);
    }

    private static PersistenceCheckInReplayRecord ToPersistenceRecord(
        ApplicationCheckInReplayRecord replayRecord) =>
        new()
        {
            EventId = replayRecord.EventId.Value,
            ClientScanId = replayRecord.ClientScanId,
            CodeFingerprint = replayRecord.CodeFingerprint,
            ScannedAtUtc = replayRecord.ScannedAtUtc,
            Accepted = replayRecord.Accepted,
            ResponseStatus = replayRecord.ResponseStatus,
            RejectionReason = replayRecord.RejectionReason,
            TicketId = replayRecord.TicketId?.Value,
            CheckedInAt = replayRecord.CheckedInAt,
            ResolvedAt = replayRecord.ResolvedAt
        };

    private static ApplicationCheckInReplayRecord ToApplicationRecord(
        PersistenceCheckInReplayRecord record) =>
        new(
            EventId.From(record.EventId),
            record.ClientScanId,
            record.CodeFingerprint,
            record.ScannedAtUtc,
            record.Accepted,
            record.ResponseStatus,
            record.RejectionReason,
            record.TicketId is { } ticketId ? TicketId.From(ticketId) : null,
            record.CheckedInAt,
            record.ResolvedAt);
}
