using Microsoft.EntityFrameworkCore;
using Solution.Application.Abstractions.Persistence;

namespace Solution.Infrastructure.Persistence;

internal sealed class ApplicationDatabaseContextFactory(
    IDbContextFactory<ApplicationDatabaseContext> databaseContextFactory)
    : IApplicationDatabaseContextFactory
{
    public async Task<IApplicationDatabaseContext> CreateApplicationDatabaseContextAsync(
        CancellationToken cancellationToken = default) =>
        await databaseContextFactory.CreateDbContextAsync(cancellationToken);
}
