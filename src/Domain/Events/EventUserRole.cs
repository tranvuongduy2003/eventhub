using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed class EventUserRole
{
    private EventUserRole()
    {
    }

    public EventId EventId { get; private set; }

    public UserId UserId { get; private set; }

    public EventRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void ChangeRole(EventRole newRole) => Role = newRole;

    public static EventUserRole Create(
        EventId eventId,
        UserId userId,
        EventRole role,
        DateTimeOffset createdAt) =>
        new()
        {
            EventId = eventId,
            UserId = userId,
            Role = role,
            CreatedAt = createdAt,
        };
}
