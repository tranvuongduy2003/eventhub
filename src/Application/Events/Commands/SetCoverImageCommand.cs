using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record SetCoverImageCommand(
    int EventId,
    Stream Content,
    string ContentType,
    string FileName) : ICommand<SetCoverImageResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.EventManagement;
}

public sealed record SetCoverImageResult(string CoverImageUrl);
