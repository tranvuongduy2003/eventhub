using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(command => command.InvitationId)
            .NotEmpty()
            .WithMessage("Invitation id cannot be empty.");

        RuleFor(command => command.Token)
            .NotEmpty()
            .WithMessage("Token is required.");
    }
}
