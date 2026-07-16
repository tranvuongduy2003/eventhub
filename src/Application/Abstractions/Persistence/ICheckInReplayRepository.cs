using EventHub.Application.Tickets;
using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface ICheckInReplayRepository
{
    Task<CheckInReplayRecord?> GetByEventAndClientScanIdAsync(
        EventId eventId,
        string clientScanId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CheckInReplayRecord replayRecord, CancellationToken cancellationToken = default);
}
