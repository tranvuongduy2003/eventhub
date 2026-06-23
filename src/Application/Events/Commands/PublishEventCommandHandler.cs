using System.Security.Cryptography;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;

namespace EventHub.Application.Events.Commands;

public sealed class PublishEventCommandHandler(
    IEventRepository eventRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<PublishEventCommand, PublishEventResult>
{
    public override async Task<Result<PublishEventResult>> Handle(
        PublishEventCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);

        var eventAggregate = await eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventAggregate is null)
        {
            return EventPublishErrors.EventNotFound;
        }

        try
        {
            var slug = await GenerateUniqueSlugAsync(eventAggregate.Title.Value, cancellationToken);

            eventAggregate.Publish(slug, clock.UtcNow);

            await eventRepository.Update(eventAggregate, cancellationToken);

            pendingDomainEventsCollector.AddRange(eventAggregate.DomainEvents);
            eventAggregate.ClearDomainEvents();

            return new PublishEventResult(
                eventAggregate.Status.ToString(),
                eventAggregate.Slug!.Value,
                eventAggregate.UpdatedAt);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Error.Validation(
                exception.Code ?? Error.ValidationFailedCode,
                exception.Message);
        }
    }

    private async Task<Slug> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = SanitizeTitle(title);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var slugText = $"{baseSlug}-{suffix}";

            var exists = await eventRepository.SlugExistsAsync(slugText, cancellationToken);
            if (!exists)
            {
                return Slug.Create(slugText);
            }
        }

        return Slug.Create($"{baseSlug}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant()}");
    }

    private static string SanitizeTitle(string title)
    {
        var chars = title.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        var slug = new string(chars);

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }
}
