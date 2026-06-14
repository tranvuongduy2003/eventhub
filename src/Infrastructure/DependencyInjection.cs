using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solution.Application.Abstractions.Auth;
using Solution.Application.Abstractions.Cache;
using Solution.Application.Abstractions.Persistence;
using Solution.Infrastructure.Auth;
using Solution.Infrastructure.Cache;
using Solution.Infrastructure.Persistence;
using Solution.Infrastructure.Persistence.Repositories;
using StackExchange.Redis;

namespace Solution.Infrastructure;

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
        services.AddScoped<ISessionStore, SessionStore>();
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();

        return services;
    }
}
