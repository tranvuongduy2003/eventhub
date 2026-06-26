using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCodeRecord>
{
    public void Configure(EntityTypeBuilder<DiscountCodeRecord> builder)
    {
        builder.ToTable("discount_codes");

        builder.HasKey(dc => dc.Id);

        builder.Property(dc => dc.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(dc => dc.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(dc => dc.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(dc => dc.Type).HasColumnName("type").HasMaxLength(32).IsRequired();
        builder.Property(dc => dc.Value).HasColumnName("value").HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(dc => dc.StartAt).HasColumnName("start_at");
        builder.Property(dc => dc.EndAt).HasColumnName("end_at");
        builder.Property(dc => dc.UsageCap).HasColumnName("usage_cap");
        builder.Property(dc => dc.UsedCount).HasColumnName("used_count").HasDefaultValue(0).IsRequired();
        builder.Property(dc => dc.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(dc => dc.UpdatedAt).HasColumnName("updated_at");
        builder.Property(dc => dc.DeletedAt).HasColumnName("deleted_at");
        builder.Property(dc => dc.RowVersion).HasColumnName("row_version").IsRowVersion().HasDefaultValue(1L);

        // Unique index on (event_id, code) — case-insensitive via normalized storage
        builder.HasIndex(dc => new { dc.EventId, dc.Code })
            .IsUnique()
            .HasDatabaseName("ix_discount_codes_event_id_code");

        builder.HasIndex(dc => dc.EventId).HasDatabaseName("ix_discount_codes_event_id");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(dc => dc.EventId)
            .HasConstraintName("fk_discount_codes_events_event_id");
    }
}
