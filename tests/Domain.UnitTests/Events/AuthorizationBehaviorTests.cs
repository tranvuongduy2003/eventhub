using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Behaviors;
using EventHub.Application.Common;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class AuthorizationBehaviorTests
{
    private static readonly EventId TestEventId = EventId.From(1);
    private static readonly UserId TestUserId = UserId.From(Guid.NewGuid());

    [Fact]
    public async Task OwnerRole_AllOperationsAllowed()
    {
        var sut = CreateBehavior(TestUserId, EventRole.Owner);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StaffWithCheckIn_CheckInOperationAllowed()
    {
        var sut = CreateBehavior(TestUserId, EventRole.Staff);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.CheckIn),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StaffWithCheckIn_EventManagementOperationDenied()
    {
        var sut = CreateBehavior(TestUserId, EventRole.Staff);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task StaffWithCheckIn_StaffManagementOperationDenied()
    {
        var sut = CreateBehavior(TestUserId, EventRole.Staff);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.StaffManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task StaffWithReporting_ReportingOperationAllowed()
    {
        var sut = CreateBehavior(TestUserId, EventRole.Staff);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.Reporting),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task StaffWithReporting_EventManagementOperationDenied()
    {
        var sut = CreateBehavior(TestUserId, EventRole.Staff);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task NoRole_Denied()
    {
        var currentUserAccessor = new FakeCurrentUserAccessor(TestUserId);
        var permissionCache = new FakePermissionCache();
        permissionCache.SetRole(TestEventId, TestUserId, null);

        var sut = new AuthorizationBehavior<TestAuthorizeRequest, Result>(
            currentUserAccessor, permissionCache, new FakeEventRepository());

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task NoRoleButOrganizerIdMatches_AllowsAsOwner()
    {
        var currentUserAccessor = new FakeCurrentUserAccessor(TestUserId);
        var permissionCache = new FakePermissionCache();
        var eventRepository = new FakeEventRepository();
        eventRepository.SetEvent(CreateEventOwnedBy(TestUserId));

        var sut = new AuthorizationBehavior<TestAuthorizeRequest, Result>(
            currentUserAccessor, permissionCache, eventRepository);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task NotAuthenticated_ReturnsUnauthorized()
    {
        var currentUserAccessor = new FakeCurrentUserAccessor(null);
        var permissionCache = new FakePermissionCache();

        var sut = new AuthorizationBehavior<TestAuthorizeRequest, Result>(
            currentUserAccessor, permissionCache, new FakeEventRepository());

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task DifferentEvents_PermissionIsolation()
    {
        var otherEventId = EventId.From(999);
        var currentUserAccessor = new FakeCurrentUserAccessor(TestUserId);
        var permissionCache = new FakePermissionCache();
        permissionCache.SetRole(TestEventId, TestUserId, EventRole.Owner);
        permissionCache.SetRole(otherEventId, TestUserId, EventRole.Staff);

        var sut = new AuthorizationBehavior<TestAuthorizeRequest, Result>(
            currentUserAccessor, permissionCache, new FakeEventRepository());

        // Owner on TestEvent — allowed
        var result1 = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();

        // Staff on otherEvent — EventManagement denied
        var result2 = await sut.Handle(
            new TestAuthorizeRequest(otherEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);
        result2.IsFailure.Should().BeTrue();
        result2.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task GenericResultType_ReturnsTypedFailure()
    {
        var currentUserAccessor = new FakeCurrentUserAccessor(TestUserId);
        var permissionCache = new FakePermissionCache();
        permissionCache.SetRole(TestEventId, TestUserId, EventRole.Staff);

        var sut = new AuthorizationBehavior<TestAuthorizeRequest, Result<string>>(
            currentUserAccessor, permissionCache, new FakeEventRepository());

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result<string>.Success("allowed")),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    private static AuthorizationBehavior<TestAuthorizeRequest, Result> CreateBehavior(
        UserId userId,
        EventRole role)
    {
        var currentUserAccessor = new FakeCurrentUserAccessor(userId);
        var permissionCache = new FakePermissionCache();
        permissionCache.SetRole(TestEventId, userId, role);

        return new AuthorizationBehavior<TestAuthorizeRequest, Result>(
            currentUserAccessor, permissionCache, new FakeEventRepository());
    }

    private sealed record TestAuthorizeRequest(
        EventId EventId,
        Permission RequiredPermission) : IAuthorizeEventOperation;

    private sealed class FakeCurrentUserAccessor(UserId? userId) : ICurrentUserAccessor
    {
        public UserId? UserId => userId;
        public bool IsAuthenticated => userId is not null;
    }

    private sealed class FakePermissionCache : IPermissionCache
    {
        private readonly Dictionary<(EventId, UserId), EventRole?> _roles = [];

        public void SetRole(EventId eventId, UserId userId, EventRole? role)
        {
            _roles[(eventId, userId)] = role;
        }

        public Task<EventRole?> GetRoleAsync(
            EventId eventId,
            UserId userId,
            CancellationToken cancellationToken)
        {
            _roles.TryGetValue((eventId, userId), out var role);
            return Task.FromResult(role);
        }
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        private readonly Dictionary<EventId, Event> _events = [];

        public void SetEvent(Event eventAggregate)
        {
            _events[eventAggregate.Id] = eventAggregate;
        }

        public Task AddAsync(Event domain, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Event?> GetByIdAsync(EventId eventId, CancellationToken cancellationToken = default)
        {
            _events.TryGetValue(eventId, out var eventAggregate);
            return Task.FromResult(eventAggregate);
        }

        public Task<Event?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Event>> GetByOrganizerAsync(
            UserId organizerId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PaginatedResult<Event>> GetPublishedUpcomingAsync(
            int page,
            int pageSize,
            DateTimeOffset now,
            EventFilter? filter = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<string>> GetDistinctLocationsAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Update(Event domain, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static Event CreateEventOwnedBy(UserId organizerId) =>
        Event.FromPersistence(
            TestEventId,
            organizerId,
            EventTitle.Create("Test event"),
            EventSchedule.Create(
                new DateTimeOffset(2026, 8, 1, 14, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 1, 16, 0, 0, TimeSpan.Zero),
                "UTC"),
            EventLocation.Create(null, true),
            null,
            EventStatus.Draft,
            null,
            null,
            null,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            1);
}
