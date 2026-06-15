using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Application.Users.Commands;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventHub.Infrastructure.Persistence;

internal sealed class UnitOfWork(ApplicationDatabaseContext databaseContext) : IUnitOfWork
{
    private const string UniqueViolationSqlState = "23505";
    private const string EmailUniqueConstraint = "ux_users_email";

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        databaseContext.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await databaseContext.Database.BeginTransactionAsync(cancellationToken);
        return new UnitOfWorkTransaction(transaction);
    }

    public bool IsConcurrencyConflict(Exception exception) =>
        exception is DbUpdateConcurrencyException;

    public bool TryMapPersistenceException(Exception exception, out Error? error)
    {
        error = null;

        if (exception is not DbUpdateException dbUpdateException)
        {
            return false;
        }

        if (dbUpdateException.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        if (postgresException.SqlState != UniqueViolationSqlState)
        {
            return false;
        }

        error = postgresException.ConstraintName switch
        {
            EmailUniqueConstraint => RegistrationErrors.EmailTaken,
            _ => null
        };

        return error is not null;
    }
}
