using EventHub.Domain.Orders;

namespace EventHub.Application.Abstractions.Persistence;

public interface IOrderIdGenerator
{
    Task<OrderId> NextIdAsync(CancellationToken cancellationToken = default);
}
