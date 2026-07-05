using System.Data;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class PostgresReservationIdGenerator(ApplicationDatabaseContext databaseContext) : IReservationIdGenerator
{
    public async Task<ReservationId> NextIdAsync(CancellationToken cancellationToken = default)
    {
        var connection = databaseContext.Database.GetDbConnection();
        var shouldClose = connection.State is not ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "select nextval(pg_get_serial_sequence('app.reservations', 'id'))";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return ReservationId.From(Convert.ToInt32(result));
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
