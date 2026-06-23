using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class DuplicateEventCommandValidator : AbstractValidator<DuplicateEventCommand>
{
    public DuplicateEventCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");
    }
}
