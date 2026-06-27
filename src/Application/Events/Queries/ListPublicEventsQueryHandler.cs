using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Abstractions.Storage;
using EventHub.Application.Common;
using EventHub.Contracts.Events;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Queries;

public sealed class ListPublicEventsQueryHandler(
    IEventRepository eventRepository,
    IObjectStorage objectStorage,
    IClock clock)
    : QueryHandler<ListPublicEventsQuery, PublicEventListingResponse>
{
    private const string Bucket = "eventhub";

    public override async Task<Result<PublicEventListingResponse>> Handle(
        ListPublicEventsQuery query,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var result = await eventRepository.GetPublishedUpcomingAsync(
            query.Page,
            query.PageSize,
            now,
            query.Filter,
            cancellationToken);

        var items = result.Items.Select(eventAggregate =>
        {
            string? coverImageUrl = eventAggregate.CoverImageRef is not null
                ? objectStorage.GetPublicUri(Bucket, eventAggregate.CoverImageRef.Value).ToString()
                : null;

            var availableTicketTypes = eventAggregate.TicketTypes
                .Where(tt => tt.Available > 0)
                .ToList();

            decimal? lowestPriceAmount = null;
            string? lowestPriceCurrency = null;
            var isSoldOut = eventAggregate.TicketTypes.Count > 0 && availableTicketTypes.Count == 0;

            if (availableTicketTypes.Count > 0)
            {
                var lowest = availableTicketTypes.MinBy(tt => tt.Price.Amount);
                lowestPriceAmount = lowest!.Price.Amount;
                lowestPriceCurrency = lowest.Price.Currency;
            }

            return new PublicEventListItemResponse(
                eventAggregate.Slug!.Value,
                eventAggregate.Title.Value,
                eventAggregate.Schedule?.StartsAt,
                eventAggregate.Schedule?.TimeZoneId,
                eventAggregate.Location.PhysicalAddress,
                eventAggregate.Location.IsOnline,
                coverImageUrl,
                lowestPriceAmount,
                lowestPriceCurrency,
                isSoldOut);
        }).ToList();

        return new PublicEventListingResponse(
            items,
            result.TotalCount,
            query.Page,
            query.PageSize);
    }
}
