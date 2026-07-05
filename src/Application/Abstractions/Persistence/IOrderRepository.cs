using EventHub.Domain.Orders;

namespace EventHub.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task AddAsync(Order domain, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    Task<List<Order>> GetPendingExpiredBeforeAsync(
        DateTimeOffset expiresBefore,
        CancellationToken cancellationToken = default);

    Task Update(Order domain, CancellationToken cancellationToken = default);
}
