using EventHub.Domain.Events;
using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class EventConfiguration : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(e => e.OrganizerId).HasColumnName("organizer_id").IsRequired();
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.ScheduleStartsAt).HasColumnName("schedule_starts_at");
        builder.Property(e => e.ScheduleEndsAt).HasColumnName("schedule_ends_at");
        builder.Property(e => e.ScheduleTimeZoneId).HasColumnName("schedule_time_zone_id").HasMaxLength(100).IsRequired();
        builder.Property(e => e.LocationPhysicalAddress).HasColumnName("location_physical_address").HasMaxLength(500);
        builder.Property(e => e.LocationIsOnline).HasColumnName("location_is_online");
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(300);
        builder.Property(e => e.CoverImageKey).HasColumnName("cover_image_key").HasMaxLength(512);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).AsRowVersion();

        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("ix_events_organizer_id");

        builder.HasIndex(e => e.Slug)
            .HasDatabaseName("ix_events_slug")
            .IsUnique()
            .HasFilter("slug IS NOT NULL");

        builder.HasOne<EventHub.Infrastructure.Persistence.Entities.UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.OrganizerId)
            .HasConstraintName("fk_events_users_organizer_id");
    }
}
