using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class ListAuditLogQueryHandler(
    IPermissionAuditEntryRepository auditEntryRepository,
    IUserRepository userRepository)
    : QueryHandler<ListAuditLogQuery, AuditLogResponse>
{
    public override async Task<Result<AuditLogResponse>> Handle(
        ListAuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(query.EventId);

        var result = await auditEntryRepository.GetByEventAsync(
            eventId,
            query.Page,
            query.PageSize,
            query.From,
            query.To,
            query.Action,
            cancellationToken);

        var items = new List<AuditLogEntryResponse>(result.Items.Count);

        foreach (var entry in result.Items)
        {
            var actor = await userRepository.GetByIdAsync(entry.ActorId, cancellationToken);
            var target = await userRepository.GetByIdAsync(entry.TargetId, cancellationToken);

            items.Add(new AuditLogEntryResponse(
                entry.Id.Value,
                actor?.DisplayName.Value ?? "Unknown",
                target?.DisplayName.Value ?? "Unknown",
                entry.Action.ToString(),
                entry.OldRole?.ToString(),
                entry.NewRole?.ToString(),
                entry.OccurredAt));
        }

        return new AuditLogResponse(items, result.TotalCount, query.Page, query.PageSize);
    }
}
