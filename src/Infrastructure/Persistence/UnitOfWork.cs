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
    private const string CheckInReplayUniqueConstraint = "ux_check_in_replays_event_id_client_scan_id";

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        databaseContext.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await databaseContext.Database.BeginTransactionAsync(cancellationToken);
        return new UnitOfWorkTransaction(transaction);
    }

    public void ClearTrackedChanges() => databaseContext.ChangeTracker.Clear();

    public bool IsConcurrencyConflict(Exception exception) =>
        exception is DbUpdateConcurrencyException
        || IsUniqueViolationForConstraint(exception, CheckInReplayUniqueConstraint);

    public bool TryMapPersistenceException(Exception exception, out Error? error)
    {
        error = null;

        if (!TryGetUniqueViolation(exception, out var postgresException))
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

    private static bool IsUniqueViolationForConstraint(Exception exception, string constraintName) =>
        TryGetUniqueViolation(exception, out var postgresException)
        && string.Equals(postgresException.ConstraintName, constraintName, StringComparison.Ordinal);

    private static bool TryGetUniqueViolation(Exception exception, out PostgresException postgresException)
    {
        postgresException = exception switch
        {
            DbUpdateException { InnerException: PostgresException innerException } => innerException,
            PostgresException directException => directException,
            _ => null!
        };

        return postgresException is not null && postgresException.SqlState == UniqueViolationSqlState;
    }
}
