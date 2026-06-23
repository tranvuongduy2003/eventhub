using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class CancelOccurrenceCommandValidator : AbstractValidator<CancelOccurrenceCommand>
{
    public CancelOccurrenceCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");

        RuleFor(c => c.OccurrenceId)
            .GreaterThan(0)
            .WithMessage("Occurrence ID must be a positive integer.");
    }
}
