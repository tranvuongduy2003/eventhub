using EventHub.Domain.Abstractions;
using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    DisplayName DisplayName,
    EmailAddress Email) : DomainEvent;
