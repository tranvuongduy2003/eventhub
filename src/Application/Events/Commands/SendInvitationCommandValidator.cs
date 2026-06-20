using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class SendInvitationCommandValidator : AbstractValidator<SendInvitationCommand>
{
    public SendInvitationCommandValidator()
    {
        RuleFor(command => command.EventId)
            .GreaterThan(0)
            .WithMessage("Event id must be a positive integer.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email address format is invalid.");

        RuleFor(command => command.ExpiresInDays)
            .InclusiveBetween(1, 30)
            .When(command => command.ExpiresInDays.HasValue)
            .WithMessage("Expiry days must be between 1 and 30.");
    }
}
