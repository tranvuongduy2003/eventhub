using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Common;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Realtime;
using EventHub.Contracts.Tickets;
using EventHub.Contracts.Users;
using EventHub.Domain.Events;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EventHub.Api.IntegrationTests.Tickets;

[Collection(IntegrationTestCollection.Name)]
public sealed class CheckInTests(IntegrationTestFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task CheckInByCode_StaffWithCheckInPermission_MarksTicketCheckedIn()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-staff");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CheckInTicketResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("checkedin");
        body.CheckedInAt.Should().Be(Now);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("CheckedIn");
        ticket.CheckedInAt.Should().Be(Now);
    }

    [Fact]
    public async Task CheckInByCode_UserWithoutCheckInPermission_Returns403()
    {
        await using var factory = CreateFactory();
        var callerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var callerId = await RegisterUserAsync(callerClient, "checkin-norole");
        var ownerId = await RegisterUserAsync(ownerClient, "checkin-owner");
        var data = await SeedDoorDataAsync(factory, ownerId, callerId, role: null, OrderStatus.Confirmed);

        using var response = await callerClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task CheckInByCode_WhenCodeBelongsToDifferentEvent_ReturnsClearReason()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-wrongevent");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var otherEventId = await SeedPublishedEventAsync(factory, staffId);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{otherEventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_WRONG_EVENT");
    }

    [Fact]
    public async Task CheckInByCode_WhenCodeIsUnknown_Returns404()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-unknown");
        var eventId = await SeedPublishedEventAsync(factory, staffId);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{eventId}/check-ins/scan",
            new CheckInTicketRequest(NewCode()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_NOT_FOUND");
    }

    [Fact]
    public async Task CheckInByCode_WhenOrderIsCancelled_ReturnsClearReason()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-cancelled");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Cancelled);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_ORDER_NOT_CONFIRMED");
    }

    [Fact]
    public async Task CheckInByCode_WhenTicketAlreadyCheckedIn_ReturnsFirstCheckInTime()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-duplicate");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var firstResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));
        using var secondResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await secondResponse.Content.ReadFromJsonAsync<ApiProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Code.Should().Be("TICKET_ALREADY_CHECKED_IN");
        problem.Detail.Should().Contain(Now.ToString("O"));
    }

    [Fact]
    public async Task CheckInByCode_ConcurrentStaffRequests_AcceptsExactlyOneTicket()
    {
        var ticketReadBarrier = new TicketReadBarrierInterceptor();
        await using var factory = CreateFactory(ticketReadBarrier);
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var firstStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var secondStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var organizerId = await RegisterUserAsync(organizerClient, "checkin-concurrent-organizer");
        var firstStaffId = await RegisterUserAsync(firstStaffClient, "checkin-concurrent-first");
        var secondStaffId = await RegisterUserAsync(secondStaffClient, "checkin-concurrent-second");
        var data = await SeedDoorDataAsync(
            factory,
            organizerId,
            firstStaffId,
            EventRole.Staff,
            OrderStatus.Confirmed);
        await GrantStaffRoleAsync(factory, data.EventId, secondStaffId);

        ticketReadBarrier.Enable();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstScan = ScanAfterStartAsync(firstStaffClient, data, start.Task);
        var secondScan = ScanAfterStartAsync(secondStaffClient, data, start.Task);

        start.SetResult();
        var attempts = await Task.WhenAll(firstScan, secondScan);

        attempts.Count(attempt => attempt.StatusCode == HttpStatusCode.OK).Should().Be(1);
        attempts.Count(attempt => attempt.StatusCode == HttpStatusCode.UnprocessableEntity).Should().Be(1);
        attempts.Single(attempt => attempt.StatusCode == HttpStatusCode.UnprocessableEntity)
            .Problem!
            .Code
            .Should()
            .Be("TICKET_ALREADY_CHECKED_IN");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("CheckedIn");
        ticket.CheckedInAt.Should().Be(Now);
        ticket.RowVersion.Should().Be(2);
    }

    [Fact]
    public async Task BatchCheckIn_OfflineSync_AcceptsFirstScanAndRejectsDuplicate()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-batch");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var response = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/sync",
            new BatchCheckInTicketsRequest(
            [
                new BatchCheckInTicketRequest("scan-1", data.Code, Now.AddMinutes(-2)),
                new BatchCheckInTicketRequest("scan-2", data.Code, Now.AddMinutes(-1)),
            ]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BatchCheckInTicketsResponse>();
        body.Should().NotBeNull();
        body!.Results.Should().HaveCount(2);
        body.Results[0].Accepted.Should().BeTrue();
        body.Results[1].Accepted.Should().BeFalse();
        body.Results[1].Reason.Should().Contain("already checked in");
        body.Results[1].Reason.Should().Contain(Now.ToString("O"));

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var replays = await databaseContext.CheckInReplays
            .Where(replay => replay.EventId == data.EventId)
            .OrderBy(replay => replay.ClientScanId)
            .ToListAsync();
        replays.Should().HaveCount(2);
        replays.Count(replay => replay.Accepted).Should().Be(1);
        replays.Single(replay => !replay.Accepted).RejectionReason.Should().Contain(Now.ToString("O"));
    }

    [Fact]
    public async Task BatchCheckIn_RepeatedMatchingScan_ReplaysOriginalResultAndStoresFingerprint()
    {
        var realtimeCheckInNotifier = new RecordingRealtimeCheckInNotifier();
        await using var factory = CreateFactory(realtimeCheckInNotifier: realtimeCheckInNotifier);
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-replay-accepted");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var clientScanId = "replay-accepted";
        var historicalScan = Now.AddYears(-1).AddTicks(7);

        var first = await SyncAsync(
            staffClient,
            data.EventId,
            new BatchCheckInTicketRequest(clientScanId, $" {data.Code} ", historicalScan));
        var replay = await SyncAsync(
            staffClient,
            data.EventId,
            new BatchCheckInTicketRequest(
                clientScanId,
                data.Code,
                historicalScan.ToOffset(TimeSpan.FromHours(7))));

        var firstResult = first.Results.Single();
        var replayResult = replay.Results.Single();
        firstResult.Accepted.Should().BeTrue();
        replayResult.Accepted.Should().BeTrue();
        firstResult.Code.Should().Be(data.Code);
        replayResult.Code.Should().Be(data.Code);
        firstResult.Ticket.Should().NotBeNull();
        replayResult.Ticket.Should().NotBeNull();
        firstResult.Ticket!.CheckedInAt.Should().Be(Now);
        replayResult.Ticket!.CheckedInAt.Should().Be(firstResult.Ticket.CheckedInAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var replayRecord = await databaseContext.CheckInReplays.SingleAsync(
            storedReplay => storedReplay.EventId == data.EventId
                && storedReplay.ClientScanId == clientScanId);
        replayRecord.Accepted.Should().BeTrue();
        replayRecord.CodeFingerprint.Should().HaveLength(64);
        replayRecord.CodeFingerprint.Should().NotContain(data.Code);
        replayRecord.ScannedAtUtc.Should().Be(Now.AddYears(-1));
        replayRecord.CheckedInAt.Should().Be(Now);

        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("CheckedIn");
        ticket.CheckedInAt.Should().Be(Now);
        ticket.RowVersion.Should().Be(2);
        realtimeCheckInNotifier.EventIds.Should().Equal(data.EventId);
    }

    [Fact]
    public async Task BatchCheckIn_RepeatedRejectedScan_ReplaysOriginalRejection()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-replay-rejected");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Cancelled);
        var request = new BatchCheckInTicketRequest("replay-rejected", data.Code, Now.AddDays(-1));

        var first = await SyncAsync(staffClient, data.EventId, request);

        await using (var stateMutationScope = factory.Services.CreateAsyncScope())
        {
            var stateMutationDatabaseContext = stateMutationScope.ServiceProvider
                .GetRequiredService<ApplicationDatabaseContext>();
            var persistedTicket = await stateMutationDatabaseContext.Tickets.SingleAsync(
                ticket => ticket.Id == data.TicketId);
            var order = await stateMutationDatabaseContext.Orders
                .AsTracking()
                .SingleAsync(order => order.Id == persistedTicket.OrderId);
            order.Status = OrderStatus.Confirmed.ToString();
            order.ConfirmedAt = Now;
            order.CancelledAt = null;
            await stateMutationDatabaseContext.SaveChangesAsync();

            var persistedOrder = await stateMutationDatabaseContext.Orders
                .AsNoTracking()
                .SingleAsync(order => order.Id == persistedTicket.OrderId);
            persistedOrder.Status.Should().Be(OrderStatus.Confirmed.ToString());
        }

        var replay = await SyncAsync(staffClient, data.EventId, request);

        var firstResult = first.Results.Single();
        var replayResult = replay.Results.Single();
        firstResult.Accepted.Should().BeFalse();
        replayResult.Accepted.Should().BeFalse();
        replayResult.Reason.Should().Be(firstResult.Reason);
        replayResult.Ticket.Should().BeNull();

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var replayRecord = await databaseContext.CheckInReplays.SingleAsync(
            storedReplay => storedReplay.EventId == data.EventId
                && storedReplay.ClientScanId == request.ClientScanId);
        replayRecord.Accepted.Should().BeFalse();
        replayRecord.RejectionReason.Should().Be(firstResult.Reason);
        replayRecord.TicketId.Should().BeNull();

        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("Valid");
        ticket.RowVersion.Should().Be(1);
    }

    [Fact]
    public async Task BatchCheckIn_ReusedClientScanIdWithChangedPayload_RejectsMismatchWithoutMutatingAlternateTicket()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-replay-mismatch");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var alternateTicket = await SeedAdditionalDoorDataAsync(factory, data);
        var clientScanId = "replay-mismatch";
        var scannedAt = Now.AddDays(-1);

        var accepted = await SyncAsync(
            staffClient,
            data.EventId,
            new BatchCheckInTicketRequest(clientScanId, data.Code, scannedAt));

        string originalFingerprint;
        DateTimeOffset originalScannedAtUtc;
        bool originalAccepted;
        string originalResponseStatus;
        string? originalRejectionReason;
        int? originalTicketId;
        DateTimeOffset? originalCheckedInAt;
        DateTimeOffset originalResolvedAt;
        await using (var snapshotScope = factory.Services.CreateAsyncScope())
        {
            var snapshotDatabaseContext = snapshotScope.ServiceProvider
                .GetRequiredService<ApplicationDatabaseContext>();
            var originalReplay = await snapshotDatabaseContext.CheckInReplays.SingleAsync(
                storedReplay => storedReplay.EventId == data.EventId
                    && storedReplay.ClientScanId == clientScanId);
            originalFingerprint = originalReplay.CodeFingerprint;
            originalScannedAtUtc = originalReplay.ScannedAtUtc;
            originalAccepted = originalReplay.Accepted;
            originalResponseStatus = originalReplay.ResponseStatus;
            originalRejectionReason = originalReplay.RejectionReason;
            originalTicketId = originalReplay.TicketId;
            originalCheckedInAt = originalReplay.CheckedInAt;
            originalResolvedAt = originalReplay.ResolvedAt;
        }

        var changedCode = await SyncAsync(
            staffClient,
            data.EventId,
            new BatchCheckInTicketRequest(clientScanId, alternateTicket.Code, scannedAt));
        var changedInstant = await SyncAsync(
            staffClient,
            data.EventId,
            new BatchCheckInTicketRequest(clientScanId, data.Code, scannedAt.AddMinutes(1)));

        accepted.Results.Single().Accepted.Should().BeTrue();
        changedCode.Results.Single().Accepted.Should().BeFalse();
        changedInstant.Results.Single().Accepted.Should().BeFalse();
        changedCode.Results.Single().Reason.Should().Contain("scan identifier");
        changedInstant.Results.Single().Reason.Should().Be(changedCode.Results.Single().Reason);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var replayRecords = await databaseContext.CheckInReplays
            .Where(storedReplay => storedReplay.EventId == data.EventId
                && storedReplay.ClientScanId == clientScanId)
            .ToListAsync();
        replayRecords.Should().ContainSingle();
        var persistedReplay = replayRecords.Single();
        persistedReplay.CodeFingerprint.Should().Be(originalFingerprint);
        persistedReplay.ScannedAtUtc.Should().Be(originalScannedAtUtc);
        persistedReplay.Accepted.Should().Be(originalAccepted);
        persistedReplay.ResponseStatus.Should().Be(originalResponseStatus);
        persistedReplay.RejectionReason.Should().Be(originalRejectionReason);
        persistedReplay.TicketId.Should().Be(originalTicketId);
        persistedReplay.CheckedInAt.Should().Be(originalCheckedInAt);
        persistedReplay.ResolvedAt.Should().Be(originalResolvedAt);

        var tickets = await databaseContext.Tickets
            .Where(ticket => ticket.Id == data.TicketId || ticket.Id == alternateTicket.TicketId)
            .OrderBy(ticket => ticket.Id)
            .ToListAsync();
        tickets.Single(ticket => ticket.Id == data.TicketId).Status.Should().Be("CheckedIn");
        tickets.Single(ticket => ticket.Id == alternateTicket.TicketId).Status.Should().Be("Valid");
        tickets.Single(ticket => ticket.Id == alternateTicket.TicketId).RowVersion.Should().Be(1);
    }

    [Fact]
    public async Task BatchCheckIn_DuplicateClientScanIdWithinBatch_ReplaysFirstResultAndRejectsMismatch()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-replay-same-batch");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var clientScanId = "same-batch-replay";
        var scannedAt = Now.AddYears(-1).AddTicks(7);

        var response = await SyncAsync(
            staffClient,
            data.EventId,
            new BatchCheckInTicketRequest(clientScanId, data.Code, scannedAt),
            new BatchCheckInTicketRequest(
                clientScanId,
                data.Code,
                scannedAt.ToOffset(TimeSpan.FromHours(7))),
            new BatchCheckInTicketRequest(clientScanId, data.Code, scannedAt.AddMinutes(1)));

        response.Results.Should().HaveCount(3);
        response.Results[0].Accepted.Should().BeTrue();
        response.Results[1].Accepted.Should().BeTrue();
        response.Results[2].Accepted.Should().BeFalse();
        response.Results[1].Ticket!.CheckedInAt.Should().Be(response.Results[0].Ticket!.CheckedInAt);
        response.Results[2].Reason.Should().Contain("scan identifier");

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.CheckInReplays.CountAsync(
            storedReplay => storedReplay.EventId == data.EventId
                && storedReplay.ClientScanId == clientScanId)).Should().Be(1);
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.RowVersion.Should().Be(2);
        ticket.CheckedInAt.Should().Be(Now);
    }

    [Fact]
    public async Task BatchCheckIn_SameClientScanIdInDifferentEvents_ResolvesIndependently()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-replay-cross-event");
        var firstEvent = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var secondEvent = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);
        var clientScanId = "cross-event-replay";

        var first = await SyncAsync(
            staffClient,
            firstEvent.EventId,
            new BatchCheckInTicketRequest(clientScanId, firstEvent.Code, Now.AddDays(-1)));
        var second = await SyncAsync(
            staffClient,
            secondEvent.EventId,
            new BatchCheckInTicketRequest(clientScanId, secondEvent.Code, Now.AddDays(-1)));

        first.Results.Single().Accepted.Should().BeTrue();
        second.Results.Single().Accepted.Should().BeTrue();

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var replays = await databaseContext.CheckInReplays
            .Where(storedReplay => storedReplay.ClientScanId == clientScanId)
            .ToListAsync();
        replays.Should().HaveCount(2);
        replays.Select(storedReplay => storedReplay.EventId)
            .Should()
            .BeEquivalentTo([firstEvent.EventId, secondEvent.EventId]);
    }

    [Fact]
    public async Task BatchCheckIn_AnonymousOrUnauthorizedRequest_CreatesNoReplayState()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var unauthorizedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var anonymousClient = factory.CreateClient();
        var ownerId = await RegisterUserAsync(ownerClient, "checkin-replay-owner");
        var unauthorizedUserId = await RegisterUserAsync(unauthorizedClient, "checkin-replay-no-role");
        var data = await SeedDoorDataAsync(
            factory,
            ownerId,
            unauthorizedUserId,
            role: null,
            OrderStatus.Confirmed);

        using var anonymousResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/sync",
            new BatchCheckInTicketsRequest(
            [
                new BatchCheckInTicketRequest("anonymous-replay", data.Code, Now.AddDays(-1))
            ]));
        using var unauthorizedResponse = await unauthorizedClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/sync",
            new BatchCheckInTicketsRequest(
            [
                new BatchCheckInTicketRequest("unauthorized-replay", data.Code, Now.AddDays(-1))
            ]));

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.CheckInReplays.CountAsync(storedReplay =>
            storedReplay.EventId == data.EventId
            && (storedReplay.ClientScanId == "anonymous-replay"
                || storedReplay.ClientScanId == "unauthorized-replay"))).Should().Be(0);
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("Valid");
        ticket.RowVersion.Should().Be(1);
    }

    [Fact]
    public async Task BatchCheckIn_ConcurrentDistinctClientScanIds_AcceptsOneAndPersistsBothOutcomes()
    {
        var ticketReadBarrier = new TicketReadBarrierInterceptor();
        await using var factory = CreateFactory(ticketReadBarrier);
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var firstStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var secondStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var organizerId = await RegisterUserAsync(organizerClient, "checkin-replay-concurrent-organizer");
        var firstStaffId = await RegisterUserAsync(firstStaffClient, "checkin-replay-concurrent-first");
        var secondStaffId = await RegisterUserAsync(secondStaffClient, "checkin-replay-concurrent-second");
        var data = await SeedDoorDataAsync(
            factory,
            organizerId,
            firstStaffId,
            EventRole.Staff,
            OrderStatus.Confirmed);
        await GrantStaffRoleAsync(factory, data.EventId, secondStaffId);
        var firstRequest = new BatchCheckInTicketRequest("distinct-key-first", data.Code, Now.AddDays(-1));
        var secondRequest = new BatchCheckInTicketRequest("distinct-key-second", data.Code, Now.AddDays(-1));

        ticketReadBarrier.Enable();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstAttempt = SyncAfterStartAsync(firstStaffClient, data.EventId, firstRequest, start.Task);
        var secondAttempt = SyncAfterStartAsync(secondStaffClient, data.EventId, secondRequest, start.Task);

        start.SetResult();
        var attempts = await Task.WhenAll(firstAttempt, secondAttempt);

        attempts.Should().OnlyContain(attempt => attempt.StatusCode == HttpStatusCode.OK);
        var results = attempts.Select(attempt => attempt.Body!.Results.Single()).ToList();
        results.Count(result => result.Accepted).Should().Be(1);
        var rejectedResult = results.Single(result => !result.Accepted);
        rejectedResult.Reason.Should().Contain("already checked in");
        rejectedResult.Reason.Should().Contain(Now.ToString("O"));

        var replayedFirst = await SyncAsync(firstStaffClient, data.EventId, firstRequest);
        var replayedSecond = await SyncAsync(secondStaffClient, data.EventId, secondRequest);
        var replayedResults = replayedFirst.Results.Concat(replayedSecond.Results).ToList();
        replayedResults.Select(result => (result.ClientScanId, result.Accepted, result.Reason))
            .Should()
            .BeEquivalentTo(results.Select(result => (result.ClientScanId, result.Accepted, result.Reason)));

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var replays = await databaseContext.CheckInReplays
            .Where(storedReplay => storedReplay.EventId == data.EventId
                && (storedReplay.ClientScanId == firstRequest.ClientScanId
                    || storedReplay.ClientScanId == secondRequest.ClientScanId))
            .ToListAsync();
        replays.Should().HaveCount(2);
        replays.Count(storedReplay => storedReplay.Accepted).Should().Be(1);
        replays.Single(storedReplay => !storedReplay.Accepted).RejectionReason.Should().Contain(Now.ToString("O"));

        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("CheckedIn");
        ticket.RowVersion.Should().Be(2);
    }

    [Fact]
    public async Task BatchCheckIn_ConcurrentMatchingClientScanId_ReplaysCommittedResultAndNotifiesOnce()
    {
        var ticketReadBarrier = new TicketReadBarrierInterceptor();
        var realtimeCheckInNotifier = new RecordingRealtimeCheckInNotifier();
        await using var factory = CreateFactory(
            interceptor: ticketReadBarrier,
            realtimeCheckInNotifier: realtimeCheckInNotifier);
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var firstStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var secondStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var organizerId = await RegisterUserAsync(organizerClient, "checkin-replay-matching-organizer");
        var firstStaffId = await RegisterUserAsync(firstStaffClient, "checkin-replay-matching-first");
        var secondStaffId = await RegisterUserAsync(secondStaffClient, "checkin-replay-matching-second");
        var data = await SeedDoorDataAsync(
            factory,
            organizerId,
            firstStaffId,
            EventRole.Staff,
            OrderStatus.Confirmed);
        await GrantStaffRoleAsync(factory, data.EventId, secondStaffId);
        var request = new BatchCheckInTicketRequest("same-key-matching-race", data.Code, Now.AddDays(-1));

        ticketReadBarrier.Enable();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstAttempt = SyncAfterStartAsync(firstStaffClient, data.EventId, request, start.Task);
        var secondAttempt = SyncAfterStartAsync(secondStaffClient, data.EventId, request, start.Task);

        start.SetResult();
        var attempts = await Task.WhenAll(firstAttempt, secondAttempt);

        attempts.Should().OnlyContain(attempt => attempt.StatusCode == HttpStatusCode.OK);
        var results = attempts.Select(attempt => attempt.Body!.Results.Single()).ToList();
        results.Should().OnlyContain(result => result.Accepted && result.Ticket != null);
        results.Select(result => result.Ticket!.CheckedInAt).Should().BeEquivalentTo([Now, Now]);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.CheckInReplays.CountAsync(storedReplay =>
            storedReplay.EventId == data.EventId
            && storedReplay.ClientScanId == request.ClientScanId)).Should().Be(1);
        var ticket = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == data.TicketId);
        ticket.Status.Should().Be("CheckedIn");
        ticket.CheckedInAt.Should().Be(Now);
        ticket.RowVersion.Should().Be(2);
        realtimeCheckInNotifier.EventIds.Should().Equal(data.EventId);
    }

    [Fact]
    public async Task BatchCheckIn_ConcurrentSameClientScanIdWithDifferentCodes_CommitsOneReplayAndRejectsMismatch()
    {
        var ticketReadBarrier = new TicketReadBarrierInterceptor();
        await using var factory = CreateFactory(ticketReadBarrier);
        var organizerClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var firstStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var secondStaffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var organizerId = await RegisterUserAsync(organizerClient, "checkin-replay-key-race-organizer");
        var firstStaffId = await RegisterUserAsync(firstStaffClient, "checkin-replay-key-race-first");
        var secondStaffId = await RegisterUserAsync(secondStaffClient, "checkin-replay-key-race-second");
        var firstTicket = await SeedDoorDataAsync(
            factory,
            organizerId,
            firstStaffId,
            EventRole.Staff,
            OrderStatus.Confirmed);
        var secondTicket = await SeedAdditionalDoorDataAsync(factory, firstTicket);
        await GrantStaffRoleAsync(factory, firstTicket.EventId, secondStaffId);
        var clientScanId = "same-key-race";
        var scannedAt = Now.AddDays(-1);
        var firstRequest = new BatchCheckInTicketRequest(clientScanId, firstTicket.Code, scannedAt);
        var secondRequest = new BatchCheckInTicketRequest(clientScanId, secondTicket.Code, scannedAt);

        ticketReadBarrier.Enable();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstAttempt = SyncAfterStartAsync(firstStaffClient, firstTicket.EventId, firstRequest, start.Task);
        var secondAttempt = SyncAfterStartAsync(secondStaffClient, firstTicket.EventId, secondRequest, start.Task);

        start.SetResult();
        var attempts = await Task.WhenAll(firstAttempt, secondAttempt);

        attempts.Should().OnlyContain(attempt => attempt.StatusCode == HttpStatusCode.OK);
        var results = attempts.Select(attempt => attempt.Body!.Results.Single()).ToList();
        results.Count(result => result.Accepted).Should().Be(1);
        var rejectedResult = results.Single(result => !result.Accepted);
        rejectedResult.Reason.Should().Contain("scan identifier");

        var replayedMismatch = await SyncAsync(
            firstStaffClient,
            firstTicket.EventId,
            new BatchCheckInTicketRequest(clientScanId, rejectedResult.Code, scannedAt));
        replayedMismatch.Results.Single().Accepted.Should().BeFalse();
        replayedMismatch.Results.Single().Reason.Should().Be(rejectedResult.Reason);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        (await databaseContext.CheckInReplays.CountAsync(storedReplay =>
            storedReplay.EventId == firstTicket.EventId
            && storedReplay.ClientScanId == clientScanId)).Should().Be(1);
        var tickets = await databaseContext.Tickets
            .Where(ticket => ticket.Id == firstTicket.TicketId || ticket.Id == secondTicket.TicketId)
            .ToListAsync();
        tickets.Count(ticket => ticket.Status == "CheckedIn").Should().Be(1);
        tickets.Count(ticket => ticket.Status == "Valid").Should().Be(1);
    }

    [Fact]
    public async Task SearchAndManualCheckIn_ByBuyerEmail_ChecksInMatchingTicket()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var staffId = await RegisterUserAsync(staffClient, "checkin-manual");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var searchResponse = await staffClient.GetAsync(
            $"/api/events/{data.EventId}/check-ins/tickets?query=buyer%40example.com");

        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var search = await searchResponse.Content.ReadFromJsonAsync<SearchCheckInTicketsResponse>();
        search.Should().NotBeNull();
        search!.Tickets.Should().ContainSingle(ticket => ticket.TicketId == data.TicketId);

        using var checkInResponse = await staffClient.PostAsync(
            $"/api/events/{data.EventId}/check-ins/tickets/{data.TicketId}",
            content: null);

        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkedIn = await checkInResponse.Content.ReadFromJsonAsync<CheckInTicketResponse>();
        checkedIn.Should().NotBeNull();
        checkedIn!.CheckedInAt.Should().Be(Now);
    }

    [Fact]
    public async Task DoorCounts_RequirePermissionAndReturnCheckedInVersusIssued()
    {
        await using var factory = CreateFactory();
        var staffClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var anonymousClient = factory.CreateClient();
        var staffId = await RegisterUserAsync(staffClient, "checkin-counts");
        var data = await SeedDoorDataAsync(factory, staffId, staffId, EventRole.Staff, OrderStatus.Confirmed);

        using var checkInResponse = await staffClient.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));
        checkInResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var countsResponse = await staffClient.GetAsync($"/api/events/{data.EventId}/check-ins/counts");
        using var unauthorizedResponse = await anonymousClient.GetAsync($"/api/events/{data.EventId}/check-ins/counts");

        countsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var counts = await countsResponse.Content.ReadFromJsonAsync<DoorCountsResponse>();
        counts.Should().NotBeNull();
        counts!.CheckedIn.Should().Be(1);
        counts.TotalIssued.Should().Be(1);
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private IntegrationTestWebApplicationFactory CreateFactory(
        IInterceptor? interceptor = null,
        IRealtimeCheckInNotifier? realtimeCheckInNotifier = null) =>
        fixture.CreateFactory(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new TestClock { UtcNow = Now });

            if (interceptor is not null)
            {
                services.AddSingleton<IInterceptor>(interceptor);
            }

            if (realtimeCheckInNotifier is not null)
            {
                services.RemoveAll<IRealtimeCheckInNotifier>();
                services.AddSingleton<IRealtimeCheckInNotifier>(realtimeCheckInNotifier);
            }

            services.RemoveAll<IHostedService>();
        });

    private static async Task<Guid> RegisterUserAsync(HttpClient client, string suffix)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest(
                $"Check-in {suffix}",
                $"{suffix}_{Guid.NewGuid():N}@example.com",
                "SecurePass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        body.Should().NotBeNull();
        return body!.UserId;
    }

    private static async Task GrantStaffRoleAsync(
        IntegrationTestWebApplicationFactory factory,
        int eventId,
        Guid staffId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        databaseContext.EventUserRoles.Add(new EventUserRoleRecord
        {
            EventId = eventId,
            UserId = staffId,
            Role = EventRole.Staff,
            CreatedAt = Now,
        });
        await databaseContext.SaveChangesAsync();
    }

    private static async Task<CheckInAttempt> ScanAfterStartAsync(
        HttpClient client,
        DoorData data,
        Task start)
    {
        await start;

        using var response = await client.PostAsJsonAsync(
            $"/api/events/{data.EventId}/check-ins/scan",
            new CheckInTicketRequest(data.Code));
        var problem = response.StatusCode == HttpStatusCode.UnprocessableEntity
            ? await response.Content.ReadFromJsonAsync<ApiProblemDetails>()
            : null;

        return new CheckInAttempt(response.StatusCode, problem);
    }

    private static async Task<BatchCheckInTicketsResponse> SyncAsync(
        HttpClient client,
        int eventId,
        params BatchCheckInTicketRequest[] tickets)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/check-ins/sync",
            new BatchCheckInTicketsRequest(tickets));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BatchCheckInTicketsResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    private static async Task<BatchCheckInAttempt> SyncAfterStartAsync(
        HttpClient client,
        int eventId,
        BatchCheckInTicketRequest ticket,
        Task start)
    {
        await start;

        using var response = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/check-ins/sync",
            new BatchCheckInTicketsRequest([ticket]));
        var body = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<BatchCheckInTicketsResponse>()
            : null;

        return new BatchCheckInAttempt(response.StatusCode, body);
    }

    private static async Task<DoorData> SeedDoorDataAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid organizerId,
        Guid? roleUserId,
        EventRole? role,
        OrderStatus orderStatus)
    {
        var eventId = await SeedPublishedEventAsync(factory, organizerId);

        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var ticketTypeId = await databaseContext.TicketTypes
            .Where(ticketType => ticketType.EventId == eventId)
            .Select(ticketType => ticketType.Id)
            .SingleAsync();

        if (role is not null)
        {
            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventId,
                UserId = roleUserId!.Value,
                Role = role.Value,
                CreatedAt = Now,
            });
        }

        var order = new OrderRecord
        {
            EventId = eventId,
            ContactName = "Buyer Example",
            ContactEmail = "buyer@example.com",
            Status = orderStatus.ToString(),
            TotalAmount = 0m,
            TotalCurrency = "VND",
            PlacedAt = Now,
            ConfirmedAt = orderStatus == OrderStatus.Confirmed ? Now : null,
            CancelledAt = orderStatus == OrderStatus.Cancelled ? Now : null,
            RowVersion = 1,
            Lines =
            [
                new OrderLineRecord
                {
                    TicketTypeId = ticketTypeId,
                    Quantity = 1,
                    UnitPriceAmount = 0m,
                    UnitPriceCurrency = "VND",
                    LineTotalAmount = 0m,
                    LineTotalCurrency = "VND",
                }
            ],
        };
        databaseContext.Orders.Add(order);
        await databaseContext.SaveChangesAsync();

        var code = NewCode();
        var ticket = new TicketRecord
        {
            EventId = eventId,
            OrderId = order.Id,
            TicketTypeId = ticketTypeId,
            Code = code,
            HolderName = "Buyer Example",
            HolderEmail = "buyer@example.com",
            Status = "Valid",
            IssuedAt = Now.AddHours(-1),
            RowVersion = 1,
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        return new DoorData(eventId, ticket.Id, code);
    }

    private static async Task<DoorData> SeedAdditionalDoorDataAsync(
        IntegrationTestWebApplicationFactory factory,
        DoorData sourceTicket)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var source = await databaseContext.Tickets.SingleAsync(ticket => ticket.Id == sourceTicket.TicketId);
        var code = NewCode();
        var ticket = new TicketRecord
        {
            EventId = source.EventId,
            OrderId = source.OrderId,
            TicketTypeId = source.TicketTypeId,
            Code = code,
            HolderName = source.HolderName,
            HolderEmail = source.HolderEmail,
            Status = "Valid",
            IssuedAt = source.IssuedAt,
            RowVersion = 1,
        };
        databaseContext.Tickets.Add(ticket);
        await databaseContext.SaveChangesAsync();

        return new DoorData(source.EventId, ticket.Id, code);
    }

    private static async Task<int> SeedPublishedEventAsync(
        IntegrationTestWebApplicationFactory factory,
        Guid organizerId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventRecord = new EventRecord
        {
            Title = $"Check-in Event {suffix}",
            OrganizerId = organizerId,
            ScheduleStartsAt = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
            ScheduleEndsAt = new DateTimeOffset(2026, 7, 15, 16, 0, 0, TimeSpan.Zero),
            ScheduleTimeZoneId = "UTC",
            LocationPhysicalAddress = "123 Door Ave",
            LocationIsOnline = false,
            Status = EventStatus.Published,
            Slug = $"check-in-{suffix}",
            CreatedAt = Now,
            UpdatedAt = Now,
        };
        databaseContext.Events.Add(eventRecord);
        await databaseContext.SaveChangesAsync();

        databaseContext.TicketTypes.Add(new TicketTypeRecord
        {
            EventId = eventRecord.Id,
            Name = "General Admission",
            PriceAmount = 0m,
            PriceCurrency = "VND",
            Capacity = 10,
            MaxPerOrder = 4,
            Sold = 1,
            Reserved = 0,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        await databaseContext.SaveChangesAsync();

        return eventRecord.Id;
    }

    private static string NewCode() => $"tk_{Guid.NewGuid():N}";

    private sealed record DoorData(int EventId, int TicketId, string Code);

    private sealed record CheckInAttempt(HttpStatusCode StatusCode, ApiProblemDetails? Problem);

    private sealed record BatchCheckInAttempt(
        HttpStatusCode StatusCode,
        BatchCheckInTicketsResponse? Body);

    private sealed class RecordingRealtimeCheckInNotifier : IRealtimeCheckInNotifier
    {
        private readonly ConcurrentQueue<int> eventIds = new();

        public IReadOnlyCollection<int> EventIds => eventIds.ToArray();

        public Task NotifyCheckInChangedAsync(
            EventId eventId,
            CancellationToken cancellationToken = default)
        {
            eventIds.Enqueue(eventId.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class TicketReadBarrierInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource bothReadsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int enabled;
        private int ticketCodeReadCount;

        public void Enable() => Interlocked.Exchange(ref enabled, 1);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref enabled) == 1 && IsTicketCodeLookup(command.CommandText))
            {
                var readNumber = Interlocked.Increment(ref ticketCodeReadCount);
                if (readNumber <= 2)
                {
                    if (readNumber == 2)
                    {
                        bothReadsCompleted.SetResult();
                    }

                    await bothReadsCompleted.Task.WaitAsync(cancellationToken);
                }
            }

            return result;
        }

        private static bool IsTicketCodeLookup(string commandText) =>
            commandText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && commandText.Contains("tickets", StringComparison.OrdinalIgnoreCase)
            && commandText.Contains("code", StringComparison.OrdinalIgnoreCase);
    }
}
