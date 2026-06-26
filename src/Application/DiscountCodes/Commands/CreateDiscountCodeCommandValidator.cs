using FluentValidation;

namespace EventHub.Application.DiscountCodes.Commands;

public sealed class CreateDiscountCodeCommandValidator : AbstractValidator<CreateDiscountCodeCommand>
{
    public CreateDiscountCodeCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");

        RuleFor(c => c.Code)
            .NotEmpty()
            .WithMessage("Discount code is required.")
            .MinimumLength(3)
            .WithMessage("Discount code must be at least 3 characters.")
            .MaximumLength(30)
            .WithMessage("Discount code must not exceed 30 characters.")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Discount code must contain only letters and digits.");

        RuleFor(c => c.Value)
            .GreaterThan(0)
            .WithMessage("Discount value must be greater than zero.");

        RuleFor(c => c.Value)
            .InclusiveBetween(1, 100)
            .When(c => c.Type == Domain.DiscountCodes.DiscountCodeType.Percentage)
            .WithMessage("Percentage discount value must be between 1 and 100.");

        RuleFor(c => c.EndAt)
            .GreaterThan(c => c.StartAt)
            .When(c => c.StartAt.HasValue && c.EndAt.HasValue)
            .WithMessage("End date must be after start date.");

        RuleFor(c => c.UsageCap)
            .GreaterThan(0)
            .When(c => c.UsageCap.HasValue)
            .WithMessage("Usage cap must be at least 1 when set.");
    }
}
