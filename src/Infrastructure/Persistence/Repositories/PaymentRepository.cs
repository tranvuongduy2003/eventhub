using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(ApplicationDatabaseContext databaseContext) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var record = PaymentPersistenceMapper.ToRecord(payment);
        await databaseContext.Payments.AddAsync(record, cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == paymentId.Value, cancellationToken);

        return record is null ? null : PaymentPersistenceMapper.ToDomain(record);
    }

    public async Task<Payment?> GetLatestByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Payments
            .AsNoTracking()
            .Where(payment => payment.OrderId == orderId.Value)
            .OrderByDescending(payment => payment.InitiatedAt)
            .ThenByDescending(payment => payment.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : PaymentPersistenceMapper.ToDomain(record);
    }

    public async Task<Payment?> GetByProviderReferenceAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.ProviderReference == providerReference, cancellationToken);

        return record is null ? null : PaymentPersistenceMapper.ToDomain(record);
    }

    public Task Update(Payment payment, CancellationToken cancellationToken = default)
    {
        var record = PaymentPersistenceMapper.ToRecord(payment);
        databaseContext.Payments.Update(record);
        return Task.CompletedTask;
    }
}
