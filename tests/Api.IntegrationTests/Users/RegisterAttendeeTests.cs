using System.Net;
using System.Net.Http.Json;
using EventHub.Api.IntegrationTests.Integration;
using EventHub.Contracts.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterAttendeeTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task RegisterAttendee_Returns201_AndSetsSessionCookie()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Attendee {suffix}";
        var request = new RegisterUserRequest(
            displayName,
            $"attendee_{suffix}@example.com",
            "SecurePass1!");

        using var registerResponse = await _client.PostAsJsonAsync("/api/attendees", request);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        registerResponse.Headers.Should().ContainKey("Set-Cookie");

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.DisplayName.Should().Be(displayName);
        registration.Email.Should().Be($"attendee_{suffix}@example.com");
        registration.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAttendee_DuplicateEmail_Returns422_EMAIL_TAKEN()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"dup_{suffix}@example.com";
        var first = new RegisterUserRequest($"First {suffix}", email, "SecurePass1!");
        var second = new RegisterUserRequest($"Second {suffix}", email, "SecurePass1!");

        using (var created = await _client.PostAsJsonAsync("/api/attendees", first))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var duplicate = await _client.PostAsJsonAsync("/api/attendees", second);

        await RegisterUserTestHelpers.AssertValidationFailedAsync(
            duplicate,
            "EMAIL_TAKEN",
            errors => errors.Should().ContainKey("email"));
    }

    [Fact]
    public async Task RegisterAttendee_OrganizerEmailTaken_Returns422_EMAIL_TAKEN()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"cross_{suffix}@example.com";

        using (var created = await _client.PostAsJsonAsync(
            "/api/users",
            new RegisterUserRequest($"Organizer {suffix}", email, "SecurePass1!")))
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        using var attendeeAttempt = await _client.PostAsJsonAsync(
            "/api/attendees",
            new RegisterUserRequest($"Attendee {suffix}", email, "SecurePass1!"));

        await RegisterUserTestHelpers.AssertValidationFailedAsync(
            attendeeAttempt,
            "EMAIL_TAKEN",
            errors => errors.Should().ContainKey("email"));
    }

    [Fact]
    public async Task RegisterAttendee_WeakPassword_Returns422()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterUserRequest(
            $"Attendee {suffix}",
            $"weak_{suffix}@example.com",
            "short");

        using var response = await _client.PostAsJsonAsync("/api/attendees", request);

        await RegisterUserTestHelpers.AssertValidationFailedAsync(response);
    }

    [Fact]
    public async Task RegisterAttendee_MalformedJson_Returns400()
    {
        using var response = await _client.PostAsync(
            "/api/attendees",
            RegisterUserTestHelpers.JsonContent("{ invalid json }"));

        await RegisterUserTestHelpers.AssertInvalidRequestAsync(response);
    }

    [Fact]
    public async Task RegisterAttendee_ThenGetCurrentUser_ReturnsAttendeeRole()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var displayName = $"Role Check {suffix}";
        var email = $"role_{suffix}@example.com";

        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/attendees",
            new RegisterUserRequest(displayName, email, "SecurePass1!"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await registerResponse.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();

        using var meResponse = await _client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentUser = await meResponse.Content.ReadFromJsonAsync<LoginUserResponse>();
        currentUser.Should().NotBeNull();
        currentUser!.UserId.Should().Be(registration!.UserId);
        currentUser.DisplayName.Should().Be(displayName);
        currentUser.Email.Should().Be(email);
        currentUser.Role.Should().Be("Attendee");
    }

    [Fact]
    public async Task RegisterAttendee_ThenLogout_EndsSession()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/attendees",
            new RegisterUserRequest(
                $"Logout {suffix}",
                $"logout_{suffix}@example.com",
                "SecurePass1!"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var meResponse = await _client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterAttendee_EmailWithSpaces_IsTrimmedAndSucceeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"  spaced_{suffix}@example.com  ";
        var request = new RegisterUserRequest($"Spaced {suffix}", email, "SecurePass1!");

        using var response = await _client.PostAsJsonAsync("/api/attendees", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var registration = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        registration.Should().NotBeNull();
        registration!.Email.Should().Be($"spaced_{suffix}@example.com");
    }
}
