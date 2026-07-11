using FluentValidation;

namespace EventHub.Application.Tickets.Commands;

public sealed class TransferTicketCommandValidator : AbstractValidator<TransferTicketCommand>
{
    public TransferTicketCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .GreaterThan(0)
            .WithMessage("Order id must be a positive integer.");

        RuleFor(command => command.TicketId)
            .GreaterThan(0)
            .WithMessage("Ticket id must be a positive integer.");

        RuleFor(command => command.RecipientName)
            .NotEmpty()
            .WithMessage("Recipient name is required.")
            .MaximumLength(200)
            .WithMessage("Recipient name must not exceed 200 characters.");

        RuleFor(command => command.RecipientEmail)
            .NotEmpty()
            .WithMessage("Recipient email is required.")
            .EmailAddress()
            .WithMessage("Recipient email is not a valid email address.");
    }
}
