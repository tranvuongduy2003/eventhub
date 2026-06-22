using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class GetEventDetailsQueryHandler(
    IEventRepository eventRepository,
    ICurrentUserAccessor currentUserAccessor)
    : QueryHandler<GetEventDetailsQuery, EventDetailsResponse>
{
    public override async Task<Result<EventDetailsResponse>> Handle(
        GetEventDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(query.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return Error.NotFound("EVENT_NOT_FOUND", "The event was not found.");
        }

        if (eventAggregate.Status == EventStatus.Draft &&
            eventAggregate.OrganizerId != currentUserAccessor.UserId)
        {
            return Error.NotFound("EVENT_NOT_FOUND", "The event was not found.");
        }

        return new EventDetailsResponse(
            eventAggregate.Id.Value,
            eventAggregate.Title.Value,
            eventAggregate.Description,
            eventAggregate.Schedule.StartsAt,
            eventAggregate.Schedule.EndsAt,
            eventAggregate.Schedule.TimeZoneId,
            eventAggregate.Location.PhysicalAddress,
            eventAggregate.Location.IsOnline,
            eventAggregate.Status.ToString(),
            eventAggregate.UpdatedAt);
    }
}
