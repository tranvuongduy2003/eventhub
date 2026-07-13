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
    ICheckInReplayRepository checkInReplayRepository,
    IClock clock,
    IPendingDomainEventsCollector pendingDomainEventsCollector)
    : CommandHandler<BatchCheckInTicketsCommand, BatchCheckInTicketsResult>
{
    private const string AcceptedStatus = "accepted";
    private const string RejectedStatus = "rejected";

    public override async Task<Result<BatchCheckInTicketsResult>> Handle(
        BatchCheckInTicketsCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = EventId.From(command.EventId);
        var results = new List<BatchCheckInTicketResult>();
        var pendingReplaysByClientScanId = new Dictionary<string, PendingReplayResolution>(StringComparer.Ordinal);

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

            var payloadIdentity = CheckInReplayPayloadIdentity.Create(code, request.ScannedAt);

            if (pendingReplaysByClientScanId.TryGetValue(request.ClientScanId, out var pendingReplay))
            {
                results.Add(payloadIdentity.Matches(pendingReplay.Replay)
                    ? pendingReplay.Result
                    : CreateRejectedResult(
                        request.ClientScanId,
                        code.Value,
                        CheckInErrors.ReplayPayloadMismatch.Message));
                continue;
            }

            var storedReplay = await checkInReplayRepository.GetByEventAndClientScanIdAsync(
                eventId,
                request.ClientScanId,
                cancellationToken);

            if (storedReplay is not null)
            {
                results.Add(payloadIdentity.Matches(storedReplay)
                    ? await ToStoredReplayResultAsync(storedReplay, code.Value, cancellationToken)
                    : CreateRejectedResult(
                        request.ClientScanId,
                        code.Value,
                        CheckInErrors.ReplayPayloadMismatch.Message));
                continue;
            }

            var pendingAcceptedReplay = FindPendingAcceptedReplay(
                pendingReplaysByClientScanId,
                payloadIdentity);
            if (pendingAcceptedReplay is not null)
            {
                var reason = pendingAcceptedReplay.Replay.CheckedInAt is { } firstCheckInAt
                    ? CheckInErrors.TicketAlreadyCheckedInAt(firstCheckInAt)
                    : CheckInErrors.TicketAlreadyCheckedIn.Message;
                var rejectedReplay = await AddReplayAsync(
                    eventId,
                    request.ClientScanId,
                    payloadIdentity,
                    CreateRejectedResult(
                        request.ClientScanId,
                        code.Value,
                        reason),
                    ticketId: null,
                    checkedInAt: null,
                    cancellationToken);
                pendingReplaysByClientScanId.Add(request.ClientScanId, rejectedReplay);
                results.Add(rejectedReplay.Result);
                continue;
            }

            var ticket = await ticketRepository.GetByCodeAsync(code, cancellationToken);
            if (ticket is null)
            {
                var rejectedReplay = await AddReplayAsync(
                    eventId,
                    request.ClientScanId,
                    payloadIdentity,
                    CreateRejectedResult(request.ClientScanId, code.Value, CheckInErrors.UnknownTicket.Message),
                    ticketId: null,
                    checkedInAt: null,
                    cancellationToken);
                pendingReplaysByClientScanId.Add(request.ClientScanId, rejectedReplay);
                results.Add(rejectedReplay.Result);
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

            var result = checkIn.IsSuccess
                ? CreateAcceptedResult(request.ClientScanId, code.Value, checkIn.Value!)
                : CreateRejectedResult(
                    request.ClientScanId,
                    code.Value,
                    CheckInErrors.ToStableReplayRejectionReason(checkIn.Error!, ticket.CheckedInAt));
            var checkedInAt = result.Ticket?.CheckedInAt;

            if (result.Accepted && checkedInAt is null)
            {
                throw new InvalidOperationException("Accepted check-in result was missing its server timestamp.");
            }

            var recordedReplay = await AddReplayAsync(
                eventId,
                request.ClientScanId,
                payloadIdentity,
                result,
                result.Accepted ? ticket.Id : null,
                checkedInAt,
                cancellationToken);
            pendingReplaysByClientScanId.Add(request.ClientScanId, recordedReplay);
            results.Add(recordedReplay.Result);
        }

        return new BatchCheckInTicketsResult(results);
    }

    private async Task<PendingReplayResolution> AddReplayAsync(
        EventId eventId,
        string clientScanId,
        CheckInReplayPayloadIdentity payloadIdentity,
        BatchCheckInTicketResult result,
        TicketId? ticketId,
        DateTimeOffset? checkedInAt,
        CancellationToken cancellationToken)
    {
        var replay = new CheckInReplayRecord(
            eventId,
            clientScanId,
            payloadIdentity.CodeFingerprint,
            payloadIdentity.ScannedAtUtc,
            result.Accepted,
            result.Status,
            result.Reason,
            ticketId,
            checkedInAt,
            clock.UtcNow);
        await checkInReplayRepository.AddAsync(replay, cancellationToken);

        return new PendingReplayResolution(replay, result);
    }

    private async Task<BatchCheckInTicketResult> ToStoredReplayResultAsync(
        CheckInReplayRecord replay,
        string canonicalCode,
        CancellationToken cancellationToken)
    {
        if (!replay.Accepted)
        {
            return CreateRejectedResult(
                replay.ClientScanId,
                canonicalCode,
                replay.RejectionReason ?? CheckInErrors.TicketCannotBeCheckedIn.Message,
                replay.ResponseStatus);
        }

        if (replay.TicketId is null || replay.CheckedInAt is null)
        {
            throw new InvalidOperationException("Accepted check-in replay is missing its original ticket result.");
        }

        var ticket = await ticketRepository.GetByIdForEventAsync(
            replay.TicketId.Value,
            replay.EventId,
            cancellationToken);
        if (ticket is null)
        {
            throw new InvalidOperationException("Accepted check-in replay references a missing ticket.");
        }

        var originalTicket = CheckInTicketProjection.ToResult(ticket) with
        {
            CheckedInAt = replay.CheckedInAt
        };

        return CreateAcceptedResult(replay.ClientScanId, canonicalCode, originalTicket, replay.ResponseStatus);
    }

    private static PendingReplayResolution? FindPendingAcceptedReplay(
        IReadOnlyDictionary<string, PendingReplayResolution> pendingReplaysByClientScanId,
        CheckInReplayPayloadIdentity payloadIdentity) =>
        pendingReplaysByClientScanId.Values.FirstOrDefault(pendingReplay =>
            pendingReplay.Replay.Accepted
            && CheckInReplayPayloadIdentity.HasSameCodeFingerprint(
                pendingReplay.Replay,
                payloadIdentity));

    private static BatchCheckInTicketResult CreateAcceptedResult(
        string clientScanId,
        string code,
        CheckInTicketResult ticket,
        string status = AcceptedStatus) =>
        new(clientScanId, code, Accepted: true, status, Reason: null, ticket);

    private static BatchCheckInTicketResult CreateRejectedResult(
        string clientScanId,
        string code,
        string reason,
        string status = RejectedStatus) =>
        new(clientScanId, code, Accepted: false, status, reason, Ticket: null);

    private sealed record PendingReplayResolution(
        CheckInReplayRecord Replay,
        BatchCheckInTicketResult Result);
}
