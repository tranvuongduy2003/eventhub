using EventHub.Domain.Payments;

namespace EventHub.Application.Abstractions.Persistence;

public interface IPaymentIdGenerator
{
    Task<PaymentId> NextIdAsync(CancellationToken cancellationToken = default);
}
