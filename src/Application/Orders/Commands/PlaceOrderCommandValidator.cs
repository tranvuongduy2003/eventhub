using FluentValidation;

namespace EventHub.Application.Orders.Commands;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event id must be a positive integer.");

        RuleFor(c => c.ContactName)
            .NotEmpty()
            .WithMessage("Contact name is required.")
            .MaximumLength(200)
            .WithMessage("Contact name must not exceed 200 characters.");

        RuleFor(c => c.ContactEmail)
            .NotEmpty()
            .WithMessage("Contact email is required.")
            .EmailAddress()
            .WithMessage("Contact email is not a valid email address.");

        RuleFor(c => c.Lines)
            .NotEmpty()
            .WithMessage("An order must contain at least one line item.");

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
