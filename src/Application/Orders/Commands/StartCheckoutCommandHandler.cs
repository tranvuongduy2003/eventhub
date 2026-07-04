using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;

namespace EventHub.Application.Orders.Commands;

public sealed class StartCheckoutCommandHandler(
    IEventRepository eventRepository,
    IClock clock)
    : CommandHandler<StartCheckoutCommand, StartCheckoutResult>
{
    public override async Task<Result<StartCheckoutResult>> Handle(
        StartCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var eventAggregate = await eventRepository.GetBySlugAsync(command.Slug, cancellationToken);
        if (eventAggregate is null || eventAggregate.Status == EventStatus.Draft)
        {
            return OrderErrors.EventNotFound;
        }

        if (eventAggregate.Status is not EventStatus.Published)
        {
            return OrderErrors.EventNotPurchasable;
        }

        var quantityByType = command.Lines
            .GroupBy(l => l.TicketTypeId)
            .ToDictionary(g => TicketTypeId.From(g.Key), g => g.Sum(l => l.Quantity));

        if (quantityByType.Count == 0)
        {
            return OrderErrors.NoItems;
        }

        var ticketTypeLookup = eventAggregate.TicketTypes.ToDictionary(t => t.Id);
        var now = clock.UtcNow;

        foreach (var (ticketTypeId, quantity) in quantityByType)
        {
            if (!ticketTypeLookup.TryGetValue(ticketTypeId, out var ticketType))
            {
                return OrderErrors.TicketTypeNotFound;
            }

            if (ticketType.SalesWindow is not null && !ticketType.SalesWindow.IsOpen(now))
            {
                return now < ticketType.SalesWindow.Start
                    ? OrderErrors.TicketTypeNotYetOnSale(ticketType.Name.Value)
                    : OrderErrors.TicketTypeSalesEnded(ticketType.Name.Value);
            }

            if (ticketType.Available <= 0)
            {
                return OrderErrors.TicketTypeSoldOutForCheckout(ticketType.Name.Value);
            }

            if (quantity > ticketType.Available)
            {
                return OrderErrors.InsufficientAvailabilityForCheckout(ticketType.Name.Value);
            }

            if (ticketType.MaxPerOrder.HasValue && quantity > ticketType.MaxPerOrder.Value)
            {
                return OrderErrors.MaxPerOrderExceeded(
                    ticketType.Name.Value,
                    ticketType.MaxPerOrder.Value);
            }
        }

        var lines = quantityByType.Select(line =>
        {
            var ticketType = ticketTypeLookup[line.Key];
            var lineTotalAmount = ticketType.Price.Amount * line.Value;

            return new StartCheckoutLineResult(
                ticketType.Id.Value,
                ticketType.Name.Value,
                line.Value,
                ticketType.Price.Amount,
                ticketType.Price.Currency,
                lineTotalAmount,
                ticketType.Price.Currency);
        }).ToList();

        var totalCurrency = lines[0].UnitPriceCurrency;
        var totalAmount = lines.Sum(l => l.LineTotalAmount);

        return new StartCheckoutResult(
            eventAggregate.Id.Value,
            eventAggregate.Slug!.Value,
            eventAggregate.Title.Value,
            totalAmount,
            totalCurrency,
            lines);
    }
}
