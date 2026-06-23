using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class DuplicateEventCommandHandler(
    IEventRepository eventRepository,
    ICurrentUserAccessor currentUserAccessor,
    IClock clock)
    : CommandHandler<DuplicateEventCommand, DuplicateEventResult>
{
    public override async Task<Result<DuplicateEventResult>> Handle(
        DuplicateEventCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var sourceEvent = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (sourceEvent is null)
        {
            return EventDuplicateErrors.SourceEventNotFound;
        }

        try
        {
            var userId = currentUserAccessor.UserId!.Value;
            var duplicatedEvent = sourceEvent.Duplicate(userId, clock.UtcNow);

            await eventRepository.AddAsync(duplicatedEvent, cancellationToken);

            return new DuplicateEventResult(
                duplicatedEvent.Status.ToString(),
                duplicatedEvent.CreatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
