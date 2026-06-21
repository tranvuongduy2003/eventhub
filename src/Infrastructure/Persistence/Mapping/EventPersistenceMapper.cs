using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence.Entities;

namespace EventHub.Infrastructure.Persistence.Mapping;

internal static class EventPersistenceMapper
{
    public static EventRecord ToRecord(Event domain) =>
        new()
        {
            Id = domain.Id.Value,
            OrganizerId = domain.OrganizerId.Value,
            Title = domain.Title.Value,
            ScheduleStartsAt = domain.Schedule.StartsAt,
            ScheduleEndsAt = domain.Schedule.EndsAt,
            ScheduleTimeZoneId = domain.Schedule.TimeZoneId,
            LocationPhysicalAddress = domain.Location.PhysicalAddress,
            LocationIsOnline = domain.Location.IsOnline,
            Status = domain.Status,
            CoverImageKey = domain.CoverImageRef?.Value,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt,
            RowVersion = domain.RowVersion,
        };

    public static Event ToDomain(EventRecord record) =>
        Event.FromPersistence(
            EventId.From(record.Id),
            UserId.From(record.OrganizerId),
            EventTitle.Create(record.Title),
            EventSchedule.Create(record.ScheduleStartsAt, record.ScheduleEndsAt, record.ScheduleTimeZoneId),
            EventLocation.Create(record.LocationPhysicalAddress, record.LocationIsOnline),
            record.Status,
            record.CoverImageKey is not null ? CoverImageRef.Create(record.CoverImageKey) : null,
            record.CreatedAt,
            record.UpdatedAt,
            record.RowVersion);
}
