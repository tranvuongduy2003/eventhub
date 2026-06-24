using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Orders;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(ApplicationDatabaseContext databaseContext) : IOrderRepository
{
    public async Task AddAsync(Order domain, CancellationToken cancellationToken = default)
    {
        var record = OrderPersistenceMapper.ToRecord(domain);
        await databaseContext.Orders.AddAsync(record, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId.Value, cancellationToken);

        if (record is null)
        {
            return null;
        }

        return OrderPersistenceMapper.ToDomain(record);
    }

    public async Task Update(Order domain, CancellationToken cancellationToken = default)
    {
        var record = OrderPersistenceMapper.ToRecord(domain);
        databaseContext.Orders.Update(record);
    }
}
