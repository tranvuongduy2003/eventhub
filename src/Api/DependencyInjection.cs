using System.Net;
using EventHub.Api.Auth;
using EventHub.Api.Hubs;
using EventHub.Api.Middleware;
using EventHub.Api.Options;
using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Realtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EventHub.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName);

        if (environment.IsDevelopment())
        {
            services.AddCors();
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IRealtimeSalesInventoryNotifier, SignalRRealtimeSalesInventoryNotifier>();

        services.AddAuthentication(SessionAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
                SessionAuthenticationDefaults.Scheme,
                null);

        services.AddAuthorization();

        return services;
    }

    public static IApplicationBuilder UseApiPipeline(
        this IApplicationBuilder application,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var allowedOrigins = application.ApplicationServices
                .GetRequiredService<IOptions<CorsOptions>>()
                .Value
                .AllowedOrigins
                .Where(IsAllowedDevelopmentOrigin)
                .ToArray();

            if (allowedOrigins.Length > 0)
            {
                application.UseCors(corsPolicyBuilder => corsPolicyBuilder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            }
        }

        application.UseMiddleware<ExceptionHandlingMiddleware>();
        application.UseMiddleware<OpenGraphMiddleware>();
        application.UseMiddleware<InvalidRequestMiddleware>();
        application.UseEventMonitoringHubOriginProtection();

        application.UseAuthentication();
        application.UseAuthorization();

        return application;
    }

    private static bool IsAllowedDevelopmentOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(uri.Host, out var address)
            && IPAddress.IsLoopback(address);
    }

    private static IApplicationBuilder UseEventMonitoringHubOriginProtection(
        this IApplicationBuilder application) =>
        application.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/hubs/events")
                || !context.WebSockets.IsWebSocketRequest
                || !context.Request.Headers.TryGetValue("Origin", out var originValues))
            {
                await next(context);
                return;
            }

            if (IsAllowedHubOrigin(context, originValues.ToString()))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        });

    private static bool IsAllowedHubOrigin(HttpContext context, string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        if (string.Equals(originUri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(originUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase)
            && originUri.Port == (context.Request.Host.Port ?? DefaultPort(context.Request.Scheme)))
        {
            return true;
        }

        var allowedOrigins = context.RequestServices
            .GetRequiredService<IOptions<CorsOptions>>()
            .Value
            .AllowedOrigins;

        return allowedOrigins.Any(allowedOrigin =>
            string.Equals(allowedOrigin, origin, StringComparison.OrdinalIgnoreCase)
            && IsAllowedDevelopmentOrigin(allowedOrigin));
    }

    private static int DefaultPort(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80;
}
