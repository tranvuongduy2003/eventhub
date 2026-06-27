using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.Events;

namespace EventHub.Application.Events.Commands;

public sealed record AddTicketTypeCommand(
    int EventId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int? MaxPerOrder,
    DateTimeOffset? SalesWindowStart,
    DateTimeOffset? SalesWindowEnd)
    : ICommand<AddTicketTypeResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.Ticketing;
}

public sealed record AddTicketTypeResult(
    int TicketTypeId,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    int Capacity,
    int? MaxPerOrder,
    DateTimeOffset? SalesWindowStart,
    DateTimeOffset? SalesWindowEnd,
    int Sold,
    int Reserved,
    DateTimeOffset CreatedAt);
