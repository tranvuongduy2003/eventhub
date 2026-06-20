using EventHub.Application.Abstractions.Auth;
using EventHub.Application.Abstractions.Cache;
using EventHub.Application.Abstractions.Email;
using EventHub.Application.Abstractions.Messaging;
using EventHub.Application.Abstractions.Payments;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Storage;
using EventHub.Infrastructure.Auth;
using EventHub.Infrastructure.Cache;
using EventHub.Infrastructure.Email;
using EventHub.Infrastructure.Messaging;
using EventHub.Infrastructure.Payments;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Repositories;
using EventHub.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EventHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<InfrastructureRegistrationOptions>? configure = null)
    {
        var options = new InfrastructureRegistrationOptions();
        configure?.Invoke(options);

        var postgresConnectionString = configuration.GetConnectionString("App");
        var redisConnectionString = configuration.GetConnectionString("Cache");

        void ConfigureApplicationDatabaseContext(DbContextOptionsBuilder contextOptions)
        {
            contextOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            if (!string.IsNullOrWhiteSpace(postgresConnectionString))
            {
                contextOptions.UseNpgsql(
                    postgresConnectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        ApplicationDatabaseContext.SchemaName));
            }
        }

        services.AddDbContext<ApplicationDatabaseContext>(ConfigureApplicationDatabaseContext);
        services.AddDbContextFactory<ApplicationDatabaseContext>(
            ConfigureApplicationDatabaseContext,
            ServiceLifetime.Scoped);

        if (options.RequireSessionCache)
        {
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Cache' is required. Configure Redis for session cache.");
            }

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<ICacheService, CacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, NoOpCacheService>();
        }

        services.AddScoped<IApplicationDatabaseContext>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDatabaseContext>());
        services.AddScoped<IApplicationDatabaseContextFactory, ApplicationDatabaseContextFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventUserRoleRepository, EventUserRoleRepository>();
        services.AddScoped<IEventInvitationRepository, EventInvitationRepository>();
        services.AddScoped<IPermissionAuditEntryRepository, PermissionAuditEntryRepository>();
        services.AddScoped<ISessionStore, SessionStore>();
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IPermissionCache, PermissionCache>();

        services.AddSingleton<IObjectStorage>(serviceProvider =>
        {
            var storageConnectionString = configuration.GetConnectionString("storage");
            if (!string.IsNullOrWhiteSpace(storageConnectionString))
            {
                return new MinioObjectStorage(storageConnectionString);
            }

            return new NoOpObjectStorage();
        });
        services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        services.AddSingleton<IPaymentGateway, NoOpPaymentGateway>();

        return services;
    }
}
