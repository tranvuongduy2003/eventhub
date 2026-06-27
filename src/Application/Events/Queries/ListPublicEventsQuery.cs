using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Common;
using EventHub.Contracts.Events;

namespace EventHub.Application.Events.Queries;

public sealed record ListPublicEventsQuery(
    int Page,
    int PageSize,
    EventFilter? Filter = null)
    : IQuery<PublicEventListingResponse>;
