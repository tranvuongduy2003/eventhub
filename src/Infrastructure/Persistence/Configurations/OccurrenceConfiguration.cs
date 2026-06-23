using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class OccurrenceConfiguration : IEntityTypeConfiguration<OccurrenceRecord>
{
    public void Configure(EntityTypeBuilder<OccurrenceRecord> builder)
    {
        builder.ToTable("occurrences");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(o => o.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(o => o.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(o => o.EndsAt).HasColumnName("ends_at").IsRequired();
        builder.Property(o => o.VenueName).HasColumnName("venue_name").HasMaxLength(300);
        builder.Property(o => o.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(o => o.EventId).HasDatabaseName("ix_occurrences_event_id");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(o => o.EventId)
            .HasConstraintName("fk_occurrences_events_event_id");
    }
}
