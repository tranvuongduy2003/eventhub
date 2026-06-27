namespace EventHub.Contracts.Events;

public sealed record PublicEventListingResponse(
    List<PublicEventListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
