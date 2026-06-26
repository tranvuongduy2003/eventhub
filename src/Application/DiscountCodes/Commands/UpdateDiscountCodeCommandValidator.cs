using FluentValidation;

namespace EventHub.Application.DiscountCodes.Commands;

public sealed class UpdateDiscountCodeCommandValidator : AbstractValidator<UpdateDiscountCodeCommand>
{
    public UpdateDiscountCodeCommandValidator()
    {
        RuleFor(c => c.EventId)
            .GreaterThan(0)
            .WithMessage("Event ID must be a positive integer.");

        RuleFor(c => c.DiscountCodeId)
            .GreaterThan(0)
            .WithMessage("Discount code ID must be a positive integer.");

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
