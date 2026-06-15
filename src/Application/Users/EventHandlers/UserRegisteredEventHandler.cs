using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;
using Microsoft.Extensions.Logging;

namespace EventHub.Application.Users.EventHandlers;

internal sealed class UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    : IDomainEventHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent domainEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "UserRegistered {UserId} {DisplayName}",
            domainEvent.UserId.Value,
            domainEvent.DisplayName.Value);

        return Task.CompletedTask;
    }
}
