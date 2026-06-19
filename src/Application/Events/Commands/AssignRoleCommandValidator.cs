using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(command => command.EventId)
            .GreaterThan(0)
            .WithMessage("Event id must be a positive integer.");

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id cannot be empty.");

        RuleFor(command => command.Role)
            .NotEmpty()
            .WithMessage("Role is required.")
            .Must(role => string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be 'Owner' or 'Staff'.");
    }
}
