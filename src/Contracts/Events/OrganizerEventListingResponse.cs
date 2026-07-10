namespace EventHub.Contracts.Events;

public sealed record OrganizerEventListingResponse(
    List<OrganizerEventListItemResponse> Items);
