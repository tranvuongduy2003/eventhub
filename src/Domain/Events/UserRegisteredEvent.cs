using Solution.Domain.Abstractions;
using Solution.Domain.Users;

namespace Solution.Domain.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    Username Username,
    EmailAddress Email) : DomainEvent;
