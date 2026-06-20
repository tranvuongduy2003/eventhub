using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Users;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventInvitationTests
{
    private static readonly EventId TestEventId = EventId.From(1);
    private static readonly UserId TestInviterId = UserId.From(Guid.NewGuid());
    private const string TestEmail = "alice@example.com";
    private const string TestTokenHash = "abc123";

    [Fact]
    public void Create_WithStaffRole_Succeeds()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(7);

        var invitation = EventInvitation.Create(
            TestEventId, TestEmail, EventRole.Staff, TestInviterId, TestTokenHash, expiresAt, now);

        invitation.EventId.Should().Be(TestEventId);
        invitation.Email.Should().Be(TestEmail);
        invitation.Role.Should().Be(EventRole.Staff);
        invitation.Status.Should().Be(InvitationStatus.Pending);
        invitation.InviterId.Should().Be(TestInviterId);
        invitation.TokenHash.Should().Be(TestTokenHash);
        invitation.CreatedAt.Should().Be(now);
        invitation.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Create_WithNonStaffRole_Throws()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(7);

        var act = () => EventInvitation.Create(
            TestEventId, TestEmail, EventRole.Owner, TestInviterId, TestTokenHash, expiresAt, now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("INVITATION_ONLY_STAFF");
    }

    [Fact]
    public void Create_WithExpiryInPast_Throws()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(-1);

        var act = () => EventInvitation.Create(
            TestEventId, TestEmail, EventRole.Staff, TestInviterId, TestTokenHash, expiresAt, now);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("INVITATION_INVALID_EXPIRY");
    }

    [Fact]
    public void Accept_FromPending_TransitionsToAccepted()
    {
        var invitation = CreatePendingInvitation();
        var acceptedAt = DateTimeOffset.UtcNow;

        invitation.Accept(acceptedAt);

        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedAt.Should().Be(acceptedAt);
    }

    [Fact]
    public void Accept_FromNonPending_Throws()
    {
        var invitation = CreatePendingInvitation();
        invitation.Revoke(DateTimeOffset.UtcNow);

        var act = () => invitation.Accept(DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("INVITATION_NOT_ACCEPTABLE");
    }

    [Fact]
    public void Revoke_FromPending_TransitionsToRevoked()
    {
        var invitation = CreatePendingInvitation();
        var revokedAt = DateTimeOffset.UtcNow;

        invitation.Revoke(revokedAt);

        invitation.Status.Should().Be(InvitationStatus.Revoked);
        invitation.RevokedAt.Should().Be(revokedAt);
    }

    [Fact]
    public void Revoke_FromNonPending_Throws()
    {
        var invitation = CreatePendingInvitation();
        invitation.Accept(DateTimeOffset.UtcNow);

        var act = () => invitation.Revoke(DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("INVITATION_NOT_ACCEPTABLE");
    }

    [Fact]
    public void MarkExpired_FromPending_TransitionsToExpired()
    {
        var invitation = CreatePendingInvitation();

        invitation.MarkExpired();

        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public void MarkExpired_FromNonPending_Throws()
    {
        var invitation = CreatePendingInvitation();
        invitation.Revoke(DateTimeOffset.UtcNow);

        var act = () => invitation.MarkExpired();

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("INVITATION_NOT_ACCEPTABLE");
    }

    private static EventInvitation CreatePendingInvitation()
    {
        var now = DateTimeOffset.UtcNow;
        return EventInvitation.Create(
            TestEventId, TestEmail, EventRole.Staff, TestInviterId, TestTokenHash, now.AddDays(7), now);
    }
}
