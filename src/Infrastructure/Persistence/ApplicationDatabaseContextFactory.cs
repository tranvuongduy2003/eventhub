using EventHub.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence;

internal sealed class ApplicationDatabaseContextFactory(
    IDbContextFactory<ApplicationDatabaseContext> databaseContextFactory)
    : IApplicationDatabaseContextFactory
{
    public async Task<IApplicationDatabaseContext> CreateApplicationDatabaseContextAsync(
        CancellationToken cancellationToken = default) =>
        await databaseContextFactory.CreateDbContextAsync(cancellationToken);
}
