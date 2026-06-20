using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class RevokeInvitationCommandValidator : AbstractValidator<RevokeInvitationCommand>
{
    public RevokeInvitationCommandValidator()
    {
        RuleFor(command => command.EventId)
            .GreaterThan(0)
            .WithMessage("Event id must be a positive integer.");

        RuleFor(command => command.InvitationId)
            .NotEmpty()
            .WithMessage("Invitation id cannot be empty.");
    }
}
