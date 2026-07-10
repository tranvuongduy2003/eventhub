using FluentValidation;

namespace EventHub.Application.Tickets.Commands;

public sealed class ResendTicketsCommandValidator : AbstractValidator<ResendTicketsCommand>
{
    public ResendTicketsCommandValidator()
    {
        RuleFor(command => command.OrderId).GreaterThan(0);
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();
    }
}
