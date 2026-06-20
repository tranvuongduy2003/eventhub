using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Persistence;

public interface IEventInvitationRepository
{
    Task<int> AddAsync(EventInvitation eventInvitation, CancellationToken cancellationToken = default);

    Task<EventInvitation?> GetByIdAsync(InvitationId invitationId, CancellationToken cancellationToken = default);

    Task<EventInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventInvitation>> GetByEventAsync(EventId eventId, CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingByEmailAndEventAsync(string normalizedEmail, EventId eventId, CancellationToken cancellationToken = default);

    Task UpdateAsync(EventInvitation eventInvitation, CancellationToken cancellationToken = default);
}
