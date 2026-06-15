using EventHub.Api.Auth;
using EventHub.Api.Middleware;
using EventHub.Api.Options;
using EventHub.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EventHub.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.AddOptions<CorsOptions>()
                .BindConfiguration(CorsOptions.SectionName);
            services.AddCors();
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

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
                .Where(IsHttpsOrigin)
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
        application.UseMiddleware<InvalidRequestMiddleware>();

        application.UseAuthentication();
        application.UseAuthorization();

        return application;
    }

    private static bool IsHttpsOrigin(string origin) =>
        !string.IsNullOrWhiteSpace(origin)
        && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
