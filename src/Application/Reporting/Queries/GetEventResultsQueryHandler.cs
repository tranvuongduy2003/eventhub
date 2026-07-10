using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Queries;

public sealed class GetEventResultsQueryHandler(IReportingRepository reportingRepository)
    : QueryHandler<GetEventResultsQuery, EventResultsResult>
{
    public override async Task<Result<EventResultsResult>> Handle(
        GetEventResultsQuery query,
        CancellationToken cancellationToken) =>
        await reportingRepository.GetEventResultsAsync(EventId.From(query.EventId), cancellationToken);
}
