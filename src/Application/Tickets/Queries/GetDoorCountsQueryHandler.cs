using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Tickets.Queries;

public sealed class GetDoorCountsQueryHandler(ITicketRepository ticketRepository)
    : QueryHandler<GetDoorCountsQuery, DoorCountsResult>
{
    public override async Task<Result<DoorCountsResult>> Handle(
        GetDoorCountsQuery query,
        CancellationToken cancellationToken)
    {
        var counts = await ticketRepository.GetDoorCountsAsync(EventId.From(query.EventId), cancellationToken);

        return new DoorCountsResult(counts.CheckedIn, counts.TotalIssued);
    }
}
