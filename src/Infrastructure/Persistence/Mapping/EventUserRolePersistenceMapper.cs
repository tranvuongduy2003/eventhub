using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class EventUserRolePersistenceMapper
{
    public static EventUserRoleRecord ToRecord(EventUserRole eventUserRole) =>
        new()
        {
            EventId = eventUserRole.EventId.Value,
            UserId = eventUserRole.UserId.Value,
            Role = eventUserRole.Role,
            CreatedAt = eventUserRole.CreatedAt,
        };

    public static EventUserRole ToDomain(EventUserRoleRecord record) =>
        EventUserRole.Create(
            EventId.From(record.EventId),
            UserId.From(record.UserId),
            record.Role,
            record.CreatedAt);
}
