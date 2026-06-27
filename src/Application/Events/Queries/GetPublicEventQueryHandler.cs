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

        var status = eventAggregate.Status.ToString();
        var purchasable = eventAggregate.Status == EventStatus.Published
            && ticketTypes.Any(tt => !tt.IsSoldOut
                && tt.SalesWindowStatus is null or "on_sale");

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
}
