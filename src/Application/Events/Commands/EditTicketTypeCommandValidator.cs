using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class EditTicketTypeCommandValidator : AbstractValidator<EditTicketTypeCommand>
{
    public EditTicketTypeCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");

        RuleFor(c => c.TicketTypeId)
            .GreaterThan(0)
            .WithMessage("Ticket type ID must be a positive integer.");

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Ticket type name is required.");

        RuleFor(c => c.PriceAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price amount must be non-negative.");

        RuleFor(c => c.PriceCurrency)
            .NotEmpty()
            .WithMessage("Currency is required.");

        RuleFor(c => c.Capacity)
            .GreaterThan(0)
            .WithMessage("Capacity must be a positive integer.");
    }
}
