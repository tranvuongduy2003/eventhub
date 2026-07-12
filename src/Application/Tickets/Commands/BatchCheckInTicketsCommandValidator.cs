using FluentValidation;

namespace EventHub.Application.Tickets.Commands;

public sealed class BatchCheckInTicketsCommandValidator : AbstractValidator<BatchCheckInTicketsCommand>
{
    public BatchCheckInTicketsCommandValidator()
    {
        RuleFor(command => command.Tickets)
            .NotEmpty()
            .Must(tickets => tickets.Count <= 100)
            .WithMessage("A sync batch can contain at most 100 scans.");

        RuleForEach(command => command.Tickets).ChildRules(ticket =>
        {
            ticket.RuleFor(item => item.ClientScanId).NotEmpty().MaximumLength(100);
            ticket.RuleFor(item => item.Code).NotEmpty().MaximumLength(200);
        });
    }
}
