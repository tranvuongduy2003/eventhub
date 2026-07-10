using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;

namespace EventHub.Application.Tickets.Queries;

public sealed class GetMyTicketsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IEventRepository eventRepository,
    ITicketRepository ticketRepository)
    : QueryHandler<GetMyTicketsQuery, List<TicketResult>>
{
    public override async Task<Result<List<TicketResult>>> Handle(
        GetMyTicketsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is null)
        {
            return Error.Unauthorized("UNAUTHORIZED", "Authentication is required.");
        }

        var user = await userRepository.GetByIdAsync(currentUserAccessor.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User was not found.");
        }

        var tickets = await ticketRepository.GetByHolderEmailAsync(user.Email.Value, cancellationToken);
        var eventIds = tickets.Select(ticket => ticket.EventId).Distinct().ToList();
        var results = new List<TicketResult>();

        foreach (var eventId in eventIds)
        {
            var eventTickets = tickets.Where(ticket => ticket.EventId == eventId).ToList();
            var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
            results.AddRange(TicketProjection.ToResults(eventTickets, eventAggregate));
        }

        return results
            .OrderBy(ticket => ticket.EventStartsAt)
            .ThenBy(ticket => ticket.TicketId)
            .ToList();
    }
}
