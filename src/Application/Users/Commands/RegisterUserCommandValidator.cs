using System.Net.Mail;
using EventHub.Application.Users;
using FluentValidation;

namespace EventHub.Application.Users.Commands;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.DisplayName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name?.Trim()))
            .WithMessage(RegistrationValidationMessages.DisplayNameRequired)
            .Must(name => name!.Trim().Length <= 64)
            .WithMessage(RegistrationValidationMessages.DisplayNameLength);

        RuleFor(command => command.Email)
            .Must(email => !string.IsNullOrWhiteSpace(NormalizeEmail(email)))
            .WithMessage(RegistrationValidationMessages.EmailRequired)
            .Must(email => NormalizeEmail(email).Length <= 254)
            .WithMessage(RegistrationValidationMessages.EmailTooLong)
            .Must(email => BeValidEmailFormat(NormalizeEmail(email)))
            .WithMessage(RegistrationValidationMessages.EmailInvalid);

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage(RegistrationValidationMessages.PasswordRequired)
            .MinimumLength(8)
            .WithMessage(RegistrationValidationMessages.PasswordTooShort)
            .Must(ContainsLetter)
            .WithMessage(RegistrationValidationMessages.PasswordMissingLetter)
            .Must(ContainsDigit)
            .WithMessage(RegistrationValidationMessages.PasswordMissingDigit)
            .Must(ContainsSpecialCharacter)
            .WithMessage(RegistrationValidationMessages.PasswordMissingSpecial);
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

    private static bool ContainsLetter(string password) =>
        password.Any(static character => char.IsAsciiLetter(character));

    private static bool ContainsDigit(string password) =>
        password.Any(static character => char.IsAsciiDigit(character));

    private static bool ContainsSpecialCharacter(string password) =>
        password.Any(character =>
            RegistrationValidationMessages.AllowedPasswordSpecialCharacters.Contains(character));
}
