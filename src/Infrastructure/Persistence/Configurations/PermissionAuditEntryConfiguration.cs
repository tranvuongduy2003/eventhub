using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class PermissionAuditEntryConfiguration : IEntityTypeConfiguration<PermissionAuditEntryRecord>
{
    public void Configure(EntityTypeBuilder<PermissionAuditEntryRecord> builder)
    {
        builder.ToTable("permission_audit_log");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(entry => entry.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(entry => entry.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(entry => entry.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(entry => entry.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entry => entry.OldRole).HasColumnName("old_role").HasConversion<string>().HasMaxLength(32);
        builder.Property(entry => entry.NewRole).HasColumnName("new_role").HasConversion<string>().HasMaxLength(32);
        builder.Property(entry => entry.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.HasIndex(entry => new { entry.EventId, entry.OccurredAt })
            .HasDatabaseName("ix_permission_audit_log_event_id_occurred_at");

        builder.HasOne(entry => entry.Actor)
            .WithMany()
            .HasForeignKey(entry => entry.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.Target)
            .WithMany()
            .HasForeignKey(entry => entry.TargetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
