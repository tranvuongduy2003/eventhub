namespace Solution.Application.Abstractions.Persistence;

using Solution.Application.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    bool IsConcurrencyConflict(Exception exception);

    bool TryMapPersistenceException(Exception exception, out Error? error);
}
