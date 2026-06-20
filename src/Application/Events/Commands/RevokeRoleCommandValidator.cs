using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(command => command.EventId)
            .GreaterThan(0)
            .WithMessage("Event id must be a positive integer.");

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id cannot be empty.");
    }
}
