using System.Net.Mail;
using FluentValidation;
using Solution.Application.Users;

namespace Solution.Application.Users.Commands;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .Must(email => !string.IsNullOrWhiteSpace(NormalizeEmail(email)))
            .WithMessage(LoginValidationMessages.EmailRequired)
            .Must(email => NormalizeEmail(email).Length <= 254)
            .WithMessage(LoginValidationMessages.EmailTooLong)
            .Must(email => BeValidEmailFormat(NormalizeEmail(email)))
            .WithMessage(LoginValidationMessages.EmailInvalid);

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage(LoginValidationMessages.PasswordRequired);
    }

    private static string NormalizeEmail(string? email) => email?.Trim() ?? string.Empty;

    private static bool BeValidEmailFormat(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

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
