namespace Solution.Application.Abstractions.Persistence;

public interface IApplicationDatabaseContextFactory
{
    Task<IApplicationDatabaseContext> CreateApplicationDatabaseContextAsync(
        CancellationToken cancellationToken = default);
}
