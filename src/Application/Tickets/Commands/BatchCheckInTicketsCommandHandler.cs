using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Tickets;

namespace EventHub.Application.Tickets.Commands;

public sealed class BatchCheckInTicketsCommandHandler(
    ITicketRepository ticketRepository,
    IOrderRepository orderRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<BatchCheckInTicketsCommand, BatchCheckInTicketsResult>
{
    public override async Task<Result<BatchCheckInTicketsResult>> Handle(
        BatchCheckInTicketsCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);
        var results = new List<BatchCheckInTicketResult>();
        var acceptedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in command.Tickets)
        {
            var codeText = request.Code?.Trim() ?? string.Empty;
            TicketCode code;
            try
            {
                code = TicketCode.Create(codeText);
            }
            catch (BusinessRuleValidationException)
            {
                results.Add(new BatchCheckInTicketResult(
                    request.ClientScanId,
                    codeText,
                    Accepted: false,
                    "rejected",
                    CheckInErrors.UnknownTicket.Message,
                    Ticket: null));
                continue;
            }

            if (acceptedCodes.Contains(code.Value))
            {
                results.Add(new BatchCheckInTicketResult(
                    request.ClientScanId,
                    code.Value,
                    Accepted: false,
                    "rejected",
                    "This ticket was already checked in by an earlier scan in this sync batch.",
                    Ticket: null));
                continue;
            }

            var ticket = await ticketRepository.GetByCodeAsync(code, cancellationToken);
            if (ticket is null)
            {
                results.Add(new BatchCheckInTicketResult(
                    request.ClientScanId,
                    code.Value,
                    Accepted: false,
                    "rejected",
                    CheckInErrors.UnknownTicket.Message,
                    Ticket: null));
                continue;
            }

            var checkIn = await CheckInTicketCommandHandlerCore.CheckInAsync(
                ticket,
                eventId,
                ticketRepository,
                orderRepository,
                clock,
                pendingDomainEventsCollector,
                cancellationToken);

            results.Add(checkIn.IsSuccess
                ? new BatchCheckInTicketResult(
                    request.ClientScanId,
                    code.Value,
                    Accepted: true,
                    "accepted",
                    Reason: null,
                    checkIn.Value)
                : new BatchCheckInTicketResult(
                    request.ClientScanId,
                    code.Value,
                    Accepted: false,
                    "rejected",
                    checkIn.Error!.Message,
                    Ticket: null));

            if (checkIn.IsSuccess)
            {
                acceptedCodes.Add(code.Value);
            }
        }

        return new BatchCheckInTicketsResult(results);
    }
}
