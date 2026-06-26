using EventHub.Domain.DiscountCodes;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class DiscountCodePersistenceMapper
{
    public static DiscountCodeRecord ToRecord(DiscountCode domain) =>
        new()
        {
            Id = domain.Id.Value,
            EventId = domain.EventId,
            Code = domain.Code,
            Type = domain.Type.ToString(),
            Value = domain.Value,
            StartAt = domain.StartAt,
            EndAt = domain.EndAt,
            UsageCap = domain.UsageCap,
            UsedCount = domain.UsedCount,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
            DeletedAt = domain.DeletedAt,
            RowVersion = domain.RowVersion,
        };

    public static DiscountCode ToDomain(DiscountCodeRecord record)
    {
        var type = Enum.Parse<DiscountCodeType>(record.Type);

        return DiscountCode.FromPersistence(
            DiscountCodeId.From(record.Id),
            record.EventId,
            record.Code,
            type,
            record.Value,
            record.StartAt,
            record.EndAt,
            record.UsageCap,
            record.UsedCount,
            record.CreatedAt,
            record.UpdatedAt,
            record.DeletedAt,
            record.RowVersion);
    }
}
