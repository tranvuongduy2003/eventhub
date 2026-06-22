using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");
    }
}
