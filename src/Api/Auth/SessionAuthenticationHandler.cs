using System.Security.Claims;
using System.Text.Encodings.Web;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EventHub.Api.Auth;

internal sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISessionStore sessionStore,
    IOptions<AuthSessionOptions> sessionOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(sessionOptions.Value.CookieName, out var sessionIdValue)
            || !Guid.TryParse(sessionIdValue, out var sessionId))
        {
            return AuthenticateResult.NoResult();
        }

        var userId = await sessionStore.ResolveUserIdAsync(sessionId, Context.RequestAborted);
        if (userId is null)
        {
            return AuthenticateResult.Fail("Session is invalid or expired.");
        }

        var claims = new[]
        {
            new Claim(SessionAuthenticationDefaults.UserIdClaimType, userId.Value.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
