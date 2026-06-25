using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class RemoveTicketTypeCommandHandler(
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<RemoveTicketTypeCommand>
{
    public override async Task<Result> Handle(
        RemoveTicketTypeCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);
        var ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return TicketTypeErrors.EventNotFound;
        }

        try
        {
            eventAggregate.RemoveTicketType(
                ticketTypeId,
                clock.UtcNow);

            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return Result.Success();
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
