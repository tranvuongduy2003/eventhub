using FluentValidation;

namespace EventHub.Application.Orders.Commands;

public sealed class StartCheckoutCommandValidator : AbstractValidator<StartCheckoutCommand>
{
    public StartCheckoutCommandValidator()
    {
        RuleFor(c => c.Slug)
            .NotEmpty()
            .WithMessage("Event slug is required.");

        RuleFor(c => c.Lines)
            .NotEmpty()
            .WithMessage("Select at least one ticket.");

        RuleForEach(c => c.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.TicketTypeId)
                .GreaterThan(0)
                .WithMessage("Ticket type id must be a positive integer.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be at least 1.");
        });
    }
}
