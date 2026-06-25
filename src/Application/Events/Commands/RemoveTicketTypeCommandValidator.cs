using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class RemoveTicketTypeCommandValidator : AbstractValidator<RemoveTicketTypeCommand>
{
    public RemoveTicketTypeCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");

        RuleFor(c => c.TicketTypeId)
            .GreaterThan(0)
            .WithMessage("Ticket type ID must be a positive integer.");
    }
}
