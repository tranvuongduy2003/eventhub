using EventHub.Domain.Events;

namespace EventHub.Application.Abstractions.Auth;

public interface IAuthorizeEventOperation
{
    EventId EventId { get; }

    Permission RequiredPermission { get; }
}
