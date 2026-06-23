using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class OccurrencePersistenceMapper
{
    public static OccurrenceRecord ToRecord(Occurrence domain, int eventId) =>
        new()
        {
            Id = domain.Id.Value,
            EventId = eventId,
            StartsAt = domain.StartsAt,
            EndsAt = domain.EndsAt,
            VenueName = domain.VenueName,
            Address = domain.Address,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
        };

    public static Occurrence ToDomain(OccurrenceRecord record) =>
        Occurrence.FromPersistence(
            OccurrenceId.From(record.Id),
            record.StartsAt,
            record.EndsAt,
            record.VenueName,
            record.Address,
            record.CreatedAt,
            record.UpdatedAt);
}
