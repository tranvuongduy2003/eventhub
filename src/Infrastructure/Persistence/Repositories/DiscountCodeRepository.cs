using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.DiscountCodes;
using EventHub.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

internal sealed class DiscountCodeRepository(ApplicationDatabaseContext databaseContext) : IDiscountCodeRepository
{
    public async Task AddAsync(DiscountCode domain, CancellationToken cancellationToken = default)
    {
        var record = DiscountCodePersistenceMapper.ToRecord(domain);
        await databaseContext.DiscountCodes.AddAsync(record, cancellationToken);
    }

    public async Task<DiscountCode?> GetByIdAsync(DiscountCodeId discountCodeId, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.DiscountCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(dc => dc.Id == discountCodeId.Value, cancellationToken);

        if (record is null)
        {
            return null;
        }

        return DiscountCodePersistenceMapper.ToDomain(record);
    }

    public async Task<DiscountCode?> GetByCodeAsync(int eventId, string normalizedCode, CancellationToken cancellationToken = default)
    {
        var record = await databaseContext.DiscountCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(dc => dc.EventId == eventId && dc.Code == normalizedCode, cancellationToken);

        if (record is null)
        {
            return null;
        }

        return DiscountCodePersistenceMapper.ToDomain(record);
    }

    public async Task<List<DiscountCode>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var records = await databaseContext.DiscountCodes
            .AsNoTracking()
            .Where(dc => dc.EventId == eventId && !dc.DeletedAt.HasValue)
            .OrderByDescending(dc => dc.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(DiscountCodePersistenceMapper.ToDomain).ToList();
    }

    public async Task Update(DiscountCode domain, CancellationToken cancellationToken = default)
    {
        var record = DiscountCodePersistenceMapper.ToRecord(domain);
        databaseContext.DiscountCodes.Update(record);
    }

    public async Task Delete(DiscountCode domain, CancellationToken cancellationToken = default)
    {
        var record = DiscountCodePersistenceMapper.ToRecord(domain);
        databaseContext.DiscountCodes.Remove(record);
    }
}
