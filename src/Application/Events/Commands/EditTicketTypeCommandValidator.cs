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

        RuleFor(c => c.MaxPerOrder)
            .GreaterThan(0)
            .When(c => c.MaxPerOrder.HasValue)
            .WithMessage("Max per order must be at least 1 when set.");

        RuleFor(c => c)
            .Must(c => c.SalesWindowStart.HasValue == c.SalesWindowEnd.HasValue)
            .WithMessage("Both start and end must be provided, or neither.")
            .WithName("SalesWindow");

        RuleFor(c => c.SalesWindowEnd)
            .GreaterThan(c => c.SalesWindowStart)
            .When(c => c.SalesWindowStart.HasValue && c.SalesWindowEnd.HasValue)
            .WithMessage("Sales window end must be after start.");
    }
}
