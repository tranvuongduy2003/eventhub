using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class ListTicketTypesQueryHandler(
    IEventRepository eventRepository,
    ICurrentUserAccessor currentUserAccessor)
    : QueryHandler<ListTicketTypesQuery, List<TicketTypeResponse>>
{
    public override async Task<Result<List<TicketTypeResponse>>> Handle(
        ListTicketTypesQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return Error.Unauthorized("UNAUTHORIZED", "Authentication is required.");
        }

        var eventAggregate = await eventRepository.GetByIdAsync(EventId.From(query.EventId), cancellationToken);
        if (eventAggregate is null)
        {
            return Error.NotFound("EVENT_NOT_FOUND", "Event was not found.");
        }

        if (eventAggregate.OrganizerId != userId)
        {
            return Error.Forbidden("INSUFFICIENT_PERMISSIONS", "Insufficient permissions.");
        }

        return eventAggregate.TicketTypes
            .OrderBy(ticketType => ticketType.Id.Value)
            .Select(ticketType => new TicketTypeResponse(
                ticketType.Id.Value,
                ticketType.Name.Value,
                ticketType.Price.Amount,
                ticketType.Price.Currency,
                ticketType.Capacity.Value,
                ticketType.MaxPerOrder,
                ticketType.SalesWindow?.Start,
                ticketType.SalesWindow?.End,
                ticketType.Sold,
                ticketType.Reserved,
                ticketType.CreatedAt))
            .ToList();
    }
}
