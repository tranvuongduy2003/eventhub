using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class GetPublicEventQueryHandler(
    IEventRepository eventRepository)
    : QueryHandler<GetPublicEventQuery, PublicEventResponse>
{
    public override async Task<Result<PublicEventResponse>> Handle(
        GetPublicEventQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(query.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null || eventAggregate.Status != EventStatus.Published)
        {
            return Error.NotFound("EVENT_NOT_FOUND", "The event was not found.");
        }

        var ticketTypes = eventAggregate.TicketTypes
            .Select(tt => new PublicTicketTypeResponse(
                tt.Id.Value,
                tt.Name.Value,
                tt.Price.Amount,
                tt.Price.Currency,
                tt.Capacity.Value,
                tt.Sold,
                tt.Reserved,
                tt.Capacity.Value - tt.Sold - tt.Reserved <= 0))
            .ToList();

        return new PublicEventResponse(
            eventAggregate.Id.Value,
            eventAggregate.Title.Value,
            eventAggregate.Description,
            eventAggregate.Schedule?.StartsAt,
            eventAggregate.Schedule?.EndsAt,
            eventAggregate.Schedule?.TimeZoneId,
            eventAggregate.Location.PhysicalAddress,
            eventAggregate.Location.IsOnline,
            ticketTypes);
    }
}
