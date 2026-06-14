namespace Solution.Application.Abstractions.Persistence;

public interface IApplicationDatabaseContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
