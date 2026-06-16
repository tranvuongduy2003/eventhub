using EventHub.Application.Abstractions.Storage;
using EventHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace EventHub.Testing.Common.Fixtures;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;
    private readonly string _redisConnectionString;
    private readonly Action<IServiceCollection>? _configureTestServices;

    public IntegrationTestWebApplicationFactory(
        string postgresConnectionString,
        string redisConnectionString,
        Action<IServiceCollection>? configureTestServices = null)
    {
        _postgresConnectionString = postgresConnectionString;
        _redisConnectionString = redisConnectionString;
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:App", _postgresConnectionString),
                new KeyValuePair<string, string?>("ConnectionStrings:Cache", _redisConnectionString)
            ]);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDatabaseContext>>();
            services.RemoveAll<IDbContextFactory<ApplicationDatabaseContext>>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IObjectStorage>();

            void ConfigureApplicationDatabaseContext(DbContextOptionsBuilder contextOptions)
            {
                contextOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                contextOptions.UseNpgsql(
                    _postgresConnectionString,
                    npgsql => npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        ApplicationDatabaseContext.SchemaName));
            }

            services.AddDbContext<ApplicationDatabaseContext>(ConfigureApplicationDatabaseContext);
            services.AddDbContextFactory<ApplicationDatabaseContext>(
                ConfigureApplicationDatabaseContext,
                ServiceLifetime.Scoped);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(_redisConnectionString));
            services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        });

        if (_configureTestServices is not null)
        {
            builder.ConfigureServices(_configureTestServices);
        }
    }
}
