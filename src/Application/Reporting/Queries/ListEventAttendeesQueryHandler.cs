using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Reporting.Queries;

public sealed class ListEventAttendeesQueryHandler(IReportingRepository reportingRepository)
    : QueryHandler<ListEventAttendeesQuery, IReadOnlyList<EventAttendeeResult>>
{
    public override async Task<Result<IReadOnlyList<EventAttendeeResult>>> Handle(
        ListEventAttendeesQuery query,
        CancellationToken cancellationToken)
    {
        var attendees = await reportingRepository.ListAttendeesAsync(EventId.From(query.EventId), cancellationToken);
        return attendees.ToList();
    }
}
