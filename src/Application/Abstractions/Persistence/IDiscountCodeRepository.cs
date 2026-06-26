using EventHub.Domain.DiscountCodes;

namespace EventHub.Application.Abstractions.Persistence;

public interface IDiscountCodeRepository
{
    Task AddAsync(DiscountCode domain, CancellationToken cancellationToken = default);

    Task<DiscountCode?> GetByIdAsync(DiscountCodeId discountCodeId, CancellationToken cancellationToken = default);

    Task<DiscountCode?> GetByCodeAsync(int eventId, string normalizedCode, CancellationToken cancellationToken = default);

    Task<List<DiscountCode>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default);

    Task Update(DiscountCode domain, CancellationToken cancellationToken = default);

    Task Delete(DiscountCode domain, CancellationToken cancellationToken = default);
}
