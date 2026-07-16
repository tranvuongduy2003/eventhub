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

    public async Task<List<Order>> GetPendingExpiredBeforeAsync(
        DateTimeOffset expiresBefore,
        CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.Status == OrderStatus.Pending.ToString()
                && order.ExpiresAt != null
                && order.ExpiresAt <= expiresBefore)
            .OrderBy(order => order.ExpiresAt)
            .ThenBy(order => order.Id)
            .ToListAsync(cancellationToken);

        return records.Select(OrderPersistenceMapper.ToDomain).ToList();
    }

    public async Task<List<Order>> GetConfirmedByEventIdAsync(
        Domain.Events.EventId eventId,
        CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.EventId == eventId.Value
                && order.Status == OrderStatus.Confirmed.ToString())
            .OrderBy(order => order.Id)
            .ToListAsync(cancellationToken);

        return records.Select(OrderPersistenceMapper.ToDomain).ToList();
    }

    public async Task Update(Order domain, CancellationToken cancellationToken = default)
    {
        var record = OrderPersistenceMapper.ToRecord(domain);
        var orderEntry = databaseContext.Orders.Update(record);
        orderEntry.Property(orderRecord => orderRecord.RowVersion).CurrentValue++;
    }
}
