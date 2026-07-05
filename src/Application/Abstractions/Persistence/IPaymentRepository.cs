using EventHub.Domain.Orders;
using EventHub.Domain.Payments;

namespace EventHub.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default);

    Task<Payment?> GetLatestByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken = default);

    Task Update(Payment payment, CancellationToken cancellationToken = default);
}
