using FluentValidation;

namespace EventHub.Application.Payments.Commands;

public sealed class ConfirmPaymentCommandValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentCommandValidator()
    {
        RuleFor(command => command.ProviderReference).NotEmpty().MaximumLength(200);
    }
}

public sealed class FailPaymentCommandValidator : AbstractValidator<FailPaymentCommand>
{
    public FailPaymentCommandValidator()
    {
        RuleFor(command => command.ProviderReference).NotEmpty().MaximumLength(200);
    }
}
