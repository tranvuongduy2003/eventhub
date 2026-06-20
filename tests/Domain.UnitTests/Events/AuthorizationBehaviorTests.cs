using EventHub.Application.Abstractions.Auth;
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
            currentUserAccessor, permissionCache);

        var result = await sut.Handle(
            new TestAuthorizeRequest(TestEventId, Permission.EventManagement),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INSUFFICIENT_PERMISSIONS");
    }

    [Fact]
    public async Task NotAuthenticated_ReturnsUnauthorized()
    {
        var currentUserAccessor = new FakeCurrentUserAccessor(null);
        var permissionCache = new FakePermissionCache();

        var sut = new AuthorizationBehavior<TestAuthorizeRequest, Result>(
            currentUserAccessor, permissionCache);

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
            currentUserAccessor, permissionCache);

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
            currentUserAccessor, permissionCache);

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
            currentUserAccessor, permissionCache);
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
}
