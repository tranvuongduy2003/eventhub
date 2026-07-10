using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;

namespace EventHub.Application.Events.Queries;

public sealed class ListOrganizerEventsQueryHandler(
    IEventRepository eventRepository,
    ICurrentUserAccessor currentUserAccessor)
    : QueryHandler<ListOrganizerEventsQuery, OrganizerEventListingResponse>
{
    public override async Task<Result<OrganizerEventListingResponse>> Handle(
        ListOrganizerEventsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return Error.Unauthorized("UNAUTHORIZED", "Authentication is required.");
        }

        var events = await eventRepository.GetByOrganizerAsync(userId, cancellationToken);

        var items = events
            .OrderByDescending(eventAggregate => eventAggregate.UpdatedAt)
            .Select(eventAggregate => new OrganizerEventListItemResponse(
                eventAggregate.Id.Value,
                eventAggregate.Title.Value,
                eventAggregate.Status.ToString(),
                eventAggregate.Schedule?.StartsAt,
                eventAggregate.Schedule?.TimeZoneId,
                eventAggregate.Location.PhysicalAddress,
                eventAggregate.Location.IsOnline,
                eventAggregate.TicketTypes.Count,
                eventAggregate.TicketTypes.Sum(ticketType => ticketType.Sold),
                eventAggregate.UpdatedAt))
            .ToList();

        return new OrganizerEventListingResponse(items);
    }
}
