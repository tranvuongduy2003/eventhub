using System.Net.Mail;
using FluentValidation;

namespace EventHub.Application.Users.Commands;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .Cascade(CascadeMode.Stop)
            .Must(name => name is null || !string.IsNullOrWhiteSpace(name.Trim()))
            .WithMessage("Display name cannot be empty.")
            .Must(name => name is null || name.Trim().Length <= 64)
            .WithMessage("Display name must not exceed 64 characters.");

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .Must(email => email is null || !string.IsNullOrWhiteSpace(email.Trim()))
            .WithMessage("Email cannot be empty.")
            .Must(email => email is null || email.Trim().Length <= 254)
            .WithMessage("Email must not exceed 254 characters.")
            .Must(email => email is null || BeValidEmailFormat(email.Trim()))
            .WithMessage("Email address format is invalid.");
    }

    private static bool BeValidEmailFormat(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
