using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class AddTicketTypeCommandHandler(
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<AddTicketTypeCommand, AddTicketTypeResult>
{
    public override async Task<Result<AddTicketTypeResult>> Handle(
        AddTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return TicketTypeErrors.EventNotFound;
        }

        try
        {
            var name = TicketName.Create(command.Name);
            var price = Money.Create(command.PriceAmount, command.PriceCurrency);
            var capacity = Capacity.Create(command.Capacity);

            SalesWindow? salesWindow = null;
            if (command.SalesWindowStart.HasValue && command.SalesWindowEnd.HasValue)
            {
                salesWindow = SalesWindow.Create(command.SalesWindowStart.Value, command.SalesWindowEnd.Value);
            }

            var ticketType = eventAggregate.AddTicketType(
                name,
                price,
                capacity,
                command.MaxPerOrder,
                salesWindow,
                clock.UtcNow);

            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new AddTicketTypeResult(
                ticketType.Id.Value,
                ticketType.Name.Value,
                ticketType.Price.Amount,
                ticketType.Price.Currency,
                ticketType.Capacity.Value,
                ticketType.MaxPerOrder,
                ticketType.SalesWindow?.Start,
                ticketType.SalesWindow?.End,
                ticketType.Sold,
                ticketType.Reserved,
                ticketType.CreatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
