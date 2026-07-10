using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Tickets.Queries;

public sealed class SearchCheckInTicketsQueryHandler(ITicketRepository ticketRepository)
    : QueryHandler<SearchCheckInTicketsQuery, IReadOnlyList<CheckInTicketResult>>
{
    private const int ResultLimit = 25;

    public override async Task<Result<IReadOnlyList<CheckInTicketResult>>> Handle(
        SearchCheckInTicketsQuery query,
        CancellationToken cancellationToken)
    {
        var searchTerm = query.Query.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return CheckInErrors.SearchTermRequired;
        }

        var tickets = await ticketRepository.SearchForCheckInAsync(
            EventId.From(query.EventId),
            searchTerm,
            ResultLimit,
            cancellationToken);

        return tickets.Select(CheckInTicketProjection.ToResult).ToList();
    }
}
