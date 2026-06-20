using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class EventInvitationRepository(ApplicationDatabaseContext databaseContext) : IEventInvitationRepository
{
    public async Task<int> AddAsync(EventInvitation eventInvitation, CancellationToken cancellationToken = default)
    {
        var record = EventInvitationPersistenceMapper.ToRecord(eventInvitation);
        await databaseContext.EventInvitations.AddAsync(record, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    public async Task<EventInvitation?> GetByIdAsync(InvitationId invitationId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.EventInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                invitation => invitation.Id == invitationId.Value,
                cancellationToken);

        return record is null ? null : EventInvitationPersistenceMapper.ToDomain(record);
    }

    public async Task<EventInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.EventInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                invitation => invitation.TokenHash == tokenHash,
                cancellationToken);

        return record is null ? null : EventInvitationPersistenceMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<EventInvitation>> GetByEventAsync(EventId eventId, CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.EventInvitations
            .AsNoTracking()
            .Where(invitation => invitation.EventId == eventId.Value)
            .ToListAsync(cancellationToken);

        return records.Select(EventInvitationPersistenceMapper.ToDomain).ToList();
    }

    public Task<bool> ExistsPendingByEmailAndEventAsync(
        string normalizedEmail,
        EventId eventId,
        CancellationToken cancellationToken = default) =>
        databaseContext.EventInvitations
            .AsNoTracking()
            .AnyAsync(
                invitation => invitation.Email == normalizedEmail
                              && invitation.EventId == eventId.Value
                              && invitation.Status == InvitationStatus.Pending,
                cancellationToken);

    public async Task UpdateAsync(EventInvitation eventInvitation, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.EventInvitations
            .AsTracking()
            .FirstOrDefaultAsync(
                invitation => invitation.Id == eventInvitation.Id.Value,
                cancellationToken);

        if (record is not null)
        {
            record.Status = eventInvitation.Status;
            record.AcceptedAt = eventInvitation.AcceptedAt;
            record.RevokedAt = eventInvitation.RevokedAt;
        }
    }
}
