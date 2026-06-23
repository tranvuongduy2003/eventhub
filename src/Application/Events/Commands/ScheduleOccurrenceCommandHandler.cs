using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class ScheduleOccurrenceCommandHandler(
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<ScheduleOccurrenceCommand, ScheduleOccurrenceResult>
{
    public override async Task<Result<ScheduleOccurrenceResult>> Handle(
        ScheduleOccurrenceCommand command,
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
            var occurrence = eventAggregate.ScheduleOccurrence(
                command.StartsAt,
                command.EndsAt,
                command.VenueName,
                command.Address,
                clock.UtcNow);

            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new ScheduleOccurrenceResult(
                occurrence.Id.Value,
                occurrence.StartsAt,
                occurrence.EndsAt,
                occurrence.VenueName,
                occurrence.Address,
                occurrence.CreatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
