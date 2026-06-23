using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class RescheduleOccurrenceCommandHandler(
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<RescheduleOccurrenceCommand, RescheduleOccurrenceResult>
{
    public override async Task<Result<RescheduleOccurrenceResult>> Handle(
        RescheduleOccurrenceCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return OccurrenceErrors.EventNotFound;
        }

        try
        {
            var occurrenceId = OccurrenceId.From(command.OccurrenceId);

            eventAggregate.RescheduleOccurrence(
                occurrenceId,
                command.StartsAt,
                command.EndsAt,
                command.VenueName,
                command.Address,
                clock.UtcNow);

            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new RescheduleOccurrenceResult(
                command.OccurrenceId,
                command.StartsAt,
                command.EndsAt,
                command.VenueName,
                command.Address,
                clock.UtcNow);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
