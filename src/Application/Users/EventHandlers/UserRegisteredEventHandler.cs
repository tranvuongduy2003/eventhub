using Microsoft.Extensions.Logging;
using Solution.Application.Abstractions.Messaging;
using Solution.Domain.Events;

namespace Solution.Application.Users.EventHandlers;

internal sealed class UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    : IDomainEventHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "UserRegistered {UserId} {Username}",
            domainEvent.UserId.Value,
            domainEvent.Username.Value);

        return Task.CompletedTask;
    }
}
