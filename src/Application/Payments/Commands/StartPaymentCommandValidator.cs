using FluentValidation;

namespace EventHub.Application.Payments.Commands;

public sealed class StartPaymentCommandValidator : AbstractValidator<StartPaymentCommand>
{
    public StartPaymentCommandValidator()
    {
        RuleFor(command => command.OrderId).GreaterThan(0);
        RuleFor(command => command.SuccessUrl).NotEmpty().MaximumLength(2048);
        RuleFor(command => command.CancelUrl).NotEmpty().MaximumLength(2048);
    }
}
