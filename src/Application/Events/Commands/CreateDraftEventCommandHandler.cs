using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class CreateDraftEventCommandHandler(
    IEventRepository eventRepository,
    ICurrentUserAccessor currentUserAccessor,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<CreateDraftEventCommand, CreateDraftEventResult>
{
    public override async Task<Result<CreateDraftEventResult>> Handle(
        CreateDraftEventCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUserAccessor.UserId is not { } userId)
        {
            return EventCreationErrors.Unauthorized;
        }

        try
        {
            var title = EventTitle.Create(command.Title);
            var schedule = EventSchedule.Create(command.StartsAt, command.EndsAt, command.TimeZoneId);
            var location = EventLocation.Create(command.PhysicalAddress, command.IsOnline);
            var createdAt = clock.UtcNow;

            var draftEvent = Event.CreateDraft(userId, title, schedule, location, createdAt);

            await eventRepository.AddAsync(draftEvent, cancellationToken);

            draftEvent.MarkAsPersisted();
            pendingDomainEventsCollector.AddRange(draftEvent.DomainEvents);
            draftEvent.ClearDomainEvents();

            return new CreateDraftEventResult(
                draftEvent.Id.Value,
                draftEvent.Status.ToString(),
                draftEvent.CreatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }
}
