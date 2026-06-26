using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.DiscountCodes;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;

namespace EventHub.Application.Orders.Commands;

public sealed class PlaceOrderCommandHandler(
    IEventRepository eventRepository,
    IOrderRepository orderRepository,
    IDiscountCodeRepository discountCodeRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private const int HoldDurationMinutes = 15;

    public override async Task<Result<PlaceOrderResult>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return OrderErrors.EventNotFound;
        }

        if (eventAggregate.Status is not EventStatus.Published)
        {
            return OrderErrors.EventNotPublished;
        }

        // Validate all ticket types exist on the event
        var ticketTypeLookup = eventAggregate.TicketTypes.ToDictionary(t => t.Id);
        var requestedTicketTypeIds = command.Lines.Select(l => TicketTypeId.From(l.TicketTypeId)).ToList();

        foreach (var ticketTypeId in requestedTicketTypeIds)
        {
            if (!ticketTypeLookup.ContainsKey(ticketTypeId))
            {
                return OrderErrors.TicketTypeNotFound;
            }
        }

        // Check availability (capacity - sold - reserved >= requested quantity)
        var quantityByType = command.Lines
            .GroupBy(l => l.TicketTypeId)
            .ToDictionary(g => TicketTypeId.From(g.Key), g => g.Sum(l => l.Quantity));

        foreach (var (ticketTypeId, requestedQty) in quantityByType)
        {
            var ticketType = ticketTypeLookup[ticketTypeId];
            var available = ticketType.Capacity.Value - ticketType.Sold - ticketType.Reserved;
            if (requestedQty > available)
            {
                return OrderErrors.InsufficientAvailability;
            }
        }

        // INV-24: per-order purchase limit
        foreach (var line in command.Lines)
        {
            var ticketType = ticketTypeLookup[TicketTypeId.From(line.TicketTypeId)];
            if (ticketType.MaxPerOrder.HasValue && line.Quantity > ticketType.MaxPerOrder.Value)
            {
                return OrderErrors.MaxPerOrderExceeded(
                    ticketType.Name.Value,
                    ticketType.MaxPerOrder.Value);
            }
        }

        try
        {
            var contact = Contact.Create(command.ContactName, command.ContactEmail);
            var now = clock.UtcNow;

            // Snapshot prices (INV-25) and build order lines
            var orderLines = command.Lines.Select(line =>
            {
                var ticketType = ticketTypeLookup[TicketTypeId.From(line.TicketTypeId)];
                return OrderLine.Create(
                    ticketType.Id,
                    line.Quantity,
                    ticketType.Price);
            }).ToList();

            // Discount code validation and computation
            DiscountCodeId? discountCodeId = null;
            Money? discountAmount = null;
            DiscountCode? discountCode = null;

            if (!string.IsNullOrWhiteSpace(command.DiscountCode))
            {
                var normalizedCode = DiscountCode.NormalizeCode(command.DiscountCode);

                discountCode = await discountCodeRepository.GetByCodeAsync(
                    command.EventId, normalizedCode, cancellationToken);

                if (discountCode is null || discountCode.DeletedAt.HasValue)
                {
                    return Error.Validation(
                        "DISCOUNT_CODE_NOT_FOUND",
                        "The discount code is invalid.");
                }

                if (discountCode.StartAt.HasValue && now < discountCode.StartAt.Value)
                {
                    return Error.Validation(
                        "DISCOUNT_CODE_NOT_YET_VALID",
                        "The discount code is not yet valid.");
                }

                if (discountCode.EndAt.HasValue && now > discountCode.EndAt.Value)
                {
                    return Error.Validation(
                        "DISCOUNT_CODE_EXPIRED",
                        "The discount code has expired.");
                }

                if (discountCode.UsageCap.HasValue && discountCode.UsedCount >= discountCode.UsageCap.Value)
                {
                    return Error.Validation(
                        "DISCOUNT_CODE_EXHAUSTED",
                        "The discount code has reached its usage cap.");
                }

                // Compute discount against the subtotal
                var subtotalCurrency = orderLines[0].LineTotal.Currency;
                var subtotalAmount = orderLines.Sum(l => l.LineTotal.Amount);
                var subtotal = Money.Create(subtotalAmount, subtotalCurrency);

                discountAmount = discountCode.ComputeDiscount(subtotal);
                discountCodeId = discountCode.Id;
            }

            var order = Order.Place(eventId, contact, orderLines, now, discountCodeId, discountAmount);

            // Reserve inventory for each ticket type line
            var expiresAt = now.AddMinutes(HoldDurationMinutes);
            var reservations = new List<Reservation>();

            foreach (var line in quantityByType)
            {
                try
                {
                    var reservation = eventAggregate.Reserve(
                        line.Key,
                        line.Value,
                        order.Id,
                        expiresAt,
                        now);

                    reservations.Add(reservation);
                }
                catch (BusinessRuleValidationException exception)
                {
                    return Error.Validation(
                        exception.Code ?? Error.ValidationFailedCode,
                        exception.Message);
                }
            }

            // Set reservation back-reference on order (use first reservation)
            if (reservations.Count > 0)
            {
                order.SetReservationId(reservations[0].Id);
            }

            // Free-order auto-confirm: if total is zero, commit reservation immediately
            if (order.Total.Amount == 0)
            {
                order.MarkConfirmed(paymentId: null, confirmedAt: now);

                foreach (var reservation in reservations)
                {
                    eventAggregate.CommitReservation(reservation.Id, now);
                }
            }

            // Mark discount code as used (same transaction — atomic)
            if (discountCode is not null)
            {
                discountCode.MarkUsed(now);
                await discountCodeRepository.Update(discountCode, cancellationToken);
            }

            await orderRepository.AddAsync(order, cancellationToken);

            // Collect domain events from both aggregates
            pendingDomainEventsCollector.AddRange(order.DomainEvents);
            order.ClearDomainEvents();
            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new PlaceOrderResult(
                order.Id.Value,
                order.Status.ToString().ToLowerInvariant(),
                order.Total.Amount,
                order.Total.Currency,
                order.PaymentId,
                order.PlacedAt,
                order.ConfirmedAt,
                order.Lines.Select(l => new PlaceOrderLineResult(
                    l.Id.Value,
                    l.TicketTypeId.Value,
                    l.Quantity,
                    l.UnitPriceSnapshot.Amount,
                    l.UnitPriceSnapshot.Currency,
                    l.LineTotal.Amount,
                    l.LineTotal.Currency)).ToList(),
                discountCode?.Code,
                discountAmount?.Amount);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
