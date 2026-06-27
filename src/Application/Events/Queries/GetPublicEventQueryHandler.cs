using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class GetPublicEventQueryHandler(
    IEventRepository eventRepository,
    IClock clock)
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

        var now = clock.UtcNow;

        var ticketTypes = eventAggregate.TicketTypes
            .Select(tt =>
            {
                string? salesWindowStatus = null;
                if (tt.SalesWindow is not null)
                {
                    salesWindowStatus = tt.SalesWindow.IsOpen(now)
                        ? "on_sale"
                        : now < tt.SalesWindow.Start
                            ? "not_yet_on_sale"
                            : "sales_ended";
                }

                return new PublicTicketTypeResponse(
                    tt.Id.Value,
                    tt.Name.Value,
                    tt.Price.Amount,
                    tt.Price.Currency,
                    tt.Capacity.Value,
                    tt.MaxPerOrder,
                    tt.Sold,
                    tt.Reserved,
                    tt.Available,
                    tt.Available <= 0,
                    tt.SalesWindow?.Start,
                    tt.SalesWindow?.End,
                    salesWindowStatus);
            })
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
