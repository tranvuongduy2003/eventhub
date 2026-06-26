using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;

namespace EventHub.Application.DiscountCodes.Commands;

public sealed record UpdateDiscountCodeCommand(
    int EventId,
    int DiscountCodeId,
    DiscountCodeType Type,
    decimal Value,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int? UsageCap)
    : ICommand<UpdateDiscountCodeResult>, IAuthorizeEventOperation
{
    EventId IAuthorizeEventOperation.EventId => Domain.Events.EventId.From(EventId);

    Permission IAuthorizeEventOperation.RequiredPermission => Permission.Ticketing;
}

public sealed record UpdateDiscountCodeResult(
    int DiscountCodeId,
    int EventId,
    string Code,
    string Type,
    decimal Value,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int? UsageCap,
    int UsedCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
