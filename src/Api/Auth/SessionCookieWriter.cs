using EventHub.Application.Options;
using Microsoft.Extensions.Options;

namespace EventHub.Api.Auth;

internal static class SessionCookieWriter
{
    public static void Append(
        HttpContext httpContext,
        Guid sessionId,
        DateTimeOffset expiresAt,
        AuthSessionOptions sessionOptions)
    {
        var maxAge = expiresAt - DateTimeOffset.UtcNow;
        if (maxAge <= TimeSpan.Zero)
        {
            maxAge = TimeSpan.FromHours(sessionOptions.ExpirationHours);
        }

        httpContext.Response.Cookies.Append(
            sessionOptions.CookieName,
            sessionId.ToString("D"),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = ResolveSameSiteMode(httpContext),
                MaxAge = maxAge,
                Path = "/",
            });
    }

    public static void Delete(HttpContext httpContext, AuthSessionOptions sessionOptions)
    {
        httpContext.Response.Cookies.Delete(
            sessionOptions.CookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = ResolveSameSiteMode(httpContext),
                Path = "/",
            });
    }

    private static SameSiteMode ResolveSameSiteMode(HttpContext httpContext) =>
        httpContext.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax;
}
