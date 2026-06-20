using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed record ListAuditLogQuery(
    int EventId,
    int Page,
    int PageSize,
    DateTimeOffset? From,
    DateTimeOffset? To,
    AuditAction? Action) : IQuery<AuditLogResponse>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.EventManagement;
}
