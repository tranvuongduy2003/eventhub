using System.Data;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class PostgresOrderIdGenerator(ApplicationDatabaseContext databaseContext) : IOrderIdGenerator
{
    public async Task<OrderId> NextIdAsync(CancellationToken cancellationToken = default)
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
            command.CommandText = "select nextval(pg_get_serial_sequence('app.orders', 'id'))";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return OrderId.From(Convert.ToInt32(result));
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
