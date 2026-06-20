using EventHub.Domain.Events;

namespace EventHub.Infrastructure.Persistence.Entities;

public sealed class EventUserRoleRecord
{
    public int EventId { get; set; }

    public Guid UserId { get; set; }

    public EventRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public UserRecord? User { get; set; }
}
