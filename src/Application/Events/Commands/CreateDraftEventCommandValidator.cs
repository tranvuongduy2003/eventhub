using EventHub.Application.Events;
using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class CreateDraftEventCommandValidator : AbstractValidator<CreateDraftEventCommand>
{
    public CreateDraftEventCommandValidator()
    {
        RuleFor(command => command.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title?.Trim()))
            .WithMessage(EventCreationValidationMessages.TitleRequired)
            .Must(title => title!.Trim().Length <= 200)
            .WithMessage(EventCreationValidationMessages.TitleTooLong);

        RuleFor(command => command.EndsAt)
            .GreaterThan(command => command.StartsAt)
            .WithMessage(EventCreationValidationMessages.EndsBeforeStart);

        RuleFor(command => command.TimeZoneId)
            .NotEmpty()
            .WithMessage(EventCreationValidationMessages.TimeZoneRequired)
            .Must(BeValidTimeZone)
            .WithMessage(EventCreationValidationMessages.TimeZoneInvalid);

        RuleFor(command => command)
            .Must(HaveValidLocation)
            .WithMessage(EventCreationValidationMessages.LocationRequired);
    }

    private static bool BeValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }

    private static bool HaveValidLocation(CreateDraftEventCommand command)
    {
        var hasAddress = !string.IsNullOrWhiteSpace(command.PhysicalAddress);
        return hasAddress || command.IsOnline;
    }
}
