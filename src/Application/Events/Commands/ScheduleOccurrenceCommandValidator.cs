using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class ScheduleOccurrenceCommandValidator : AbstractValidator<ScheduleOccurrenceCommand>
{
    public ScheduleOccurrenceCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");

        RuleFor(c => c.EndsAt)
            .GreaterThan(c => c.StartsAt)
            .WithMessage("End time must be after start time.");
    }
}
