using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Solution.Api.IntegrationTests.Integration;
using Solution.Testing.Common.Fixtures;

namespace Solution.Api.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterUserSessionTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task GetCurrentUser_WithoutSession_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
