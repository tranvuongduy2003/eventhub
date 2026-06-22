using FluentValidation;

namespace EventHub.Application.Events.Commands;

public sealed class EditEventDetailsCommandValidator : AbstractValidator<EditEventDetailsCommand>
{
    public EditEventDetailsCommandValidator()
    {
        RuleFor(command => command.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title?.Trim()))
            .WithMessage("Event title is required.")
            .Must(title => title!.Trim().Length <= 200)
            .WithMessage("Event title must be between 1 and 200 characters.");

        RuleFor(command => command.EndsAt)
            .GreaterThan(command => command.StartsAt)
            .WithMessage("Event end time must be after start time.");

        RuleFor(command => command.TimeZoneId)
            .NotEmpty()
            .WithMessage("Time zone is required.")
            .Must(BeValidTimeZone)
            .WithMessage("Time zone is not valid.");

        RuleFor(command => command)
            .Must(HaveValidLocation)
            .WithMessage("Event must have a physical address or be marked as online.");

        RuleFor(command => command.Description)
            .Must(desc => desc is null || desc.Length <= 2000)
            .WithMessage("Description must be 2000 characters or less.");
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

    private static bool HaveValidLocation(EditEventDetailsCommand command)
    {
        var hasAddress = !string.IsNullOrWhiteSpace(command.PhysicalAddress);
        return hasAddress || command.IsOnline;
    }
}
