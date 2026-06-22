using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class EditEventDetailsCommandHandler(
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<EditEventDetailsCommand, EditEventDetailsResult>
{
    public override async Task<Result<EditEventDetailsResult>> Handle(
        EditEventDetailsCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return EventEditErrors.EventNotFound;
        }

        try
        {
            var title = EventTitle.Create(command.Title);
            var schedule = EventSchedule.Create(command.StartsAt, command.EndsAt, command.TimeZoneId);
            var location = EventLocation.Create(command.PhysicalAddress, command.IsOnline);

            eventAggregate.UpdateDetails(title, schedule, location, command.Description, clock.UtcNow);

            eventRepository.Update(eventAggregate);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new EditEventDetailsResult(
                eventAggregate.Status.ToString(),
                eventAggregate.UpdatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
