using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class EventInvitationPersistenceMapper
{
    public static EventInvitationRecord ToRecord(EventInvitation eventInvitation) =>
        new()
        {
            Id = eventInvitation.Id.Value,
            EventId = eventInvitation.EventId.Value,
            Email = eventInvitation.Email,
            Role = eventInvitation.Role,
            TokenHash = eventInvitation.TokenHash,
            Status = eventInvitation.Status,
            InviterId = eventInvitation.InviterId.Value,
            CreatedAt = eventInvitation.CreatedAt,
            ExpiresAt = eventInvitation.ExpiresAt,
            AcceptedAt = eventInvitation.AcceptedAt,
            RevokedAt = eventInvitation.RevokedAt,
        };

    public static EventInvitation ToDomain(EventInvitationRecord record) =>
        EventInvitation.FromPersistence(
            InvitationId.From(record.Id),
            EventId.From(record.EventId),
            record.Email,
            record.Role,
            record.TokenHash,
            record.Status,
            UserId.From(record.InviterId),
            record.CreatedAt,
            record.ExpiresAt,
            record.AcceptedAt,
            record.RevokedAt);
}
