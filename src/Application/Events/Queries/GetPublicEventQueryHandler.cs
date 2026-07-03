using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class GetPublicEventQueryHandler(
    IEventRepository eventRepository,
    IObjectStorage objectStorage,
    IClock clock)
    : QueryHandler<GetPublicEventQuery, PublicEventResponse>
{
    private const string Bucket = "eventhub";

    public override async Task<Result<PublicEventResponse>> Handle(
        GetPublicEventQuery query,
        CancellationToken cancellationToken)
    {
        var eventAggregate = await eventRepository.GetBySlugAsync(query.Slug, cancellationToken);
        if (eventAggregate is null || eventAggregate.Status == EventStatus.Draft)
        {
            return Error.NotFound("EVENT_NOT_FOUND", "The event was not found.");
        }

        var now = clock.UtcNow;

        string? coverImageUrl = eventAggregate.CoverImageRef is not null
            ? objectStorage.GetPublicUri(Bucket, eventAggregate.CoverImageRef.Value).ToString()
            : null;

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

                var (isPurchasable, availabilityState, availabilityReason) =
                    GetBuyerAvailability(eventAggregate.Status, tt, salesWindowStatus);

                return new PublicTicketTypeResponse(
                    tt.Id.Value,
                    tt.Name.Value,
                    tt.Price.Amount,
                    tt.Price.Currency,
                    tt.MaxPerOrder,
                    isPurchasable,
                    availabilityState,
                    availabilityReason,
                    tt.SalesWindow?.Start,
                    tt.SalesWindow?.End,
                    salesWindowStatus);
            })
            .ToList();

        var status = eventAggregate.Status.ToString();
        var purchasable = eventAggregate.Status == EventStatus.Published
            && ticketTypes.Any(tt => tt.IsPurchasable);

        return new PublicEventResponse(
            eventAggregate.Slug!.Value,
            eventAggregate.Title.Value,
            eventAggregate.Description,
            eventAggregate.Schedule?.StartsAt,
            eventAggregate.Schedule?.EndsAt,
            eventAggregate.Schedule?.TimeZoneId,
            eventAggregate.Location.PhysicalAddress,
            eventAggregate.Location.IsOnline,
            coverImageUrl,
            status,
            purchasable,
            ticketTypes);
    }

    private static (bool IsPurchasable, string State, string Reason) GetBuyerAvailability(
        EventStatus eventStatus,
        TicketType ticketType,
        string? salesWindowStatus)
    {
        if (eventStatus is not EventStatus.Published)
        {
            return (false, "unavailable", "This event is not currently open for ticket sales.");
        }

        if (salesWindowStatus == "not_yet_on_sale")
        {
            return (false, "not_yet_on_sale", "Sales for this ticket type have not started yet.");
        }

        if (salesWindowStatus == "sales_ended")
        {
            return (false, "sales_ended", "Sales for this ticket type have ended.");
        }

        if (ticketType.Available <= 0)
        {
            return (false, "sold_out", "This ticket type is sold out.");
        }

        if (ticketType.Available <= 10)
        {
            return (true, "limited", "Limited availability.");
        }

        return (true, "available", "Available.");
    }
}
