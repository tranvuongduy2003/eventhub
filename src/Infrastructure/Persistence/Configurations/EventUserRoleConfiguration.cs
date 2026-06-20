using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class EventUserRoleConfiguration : IEntityTypeConfiguration<EventUserRoleRecord>
{
    public void Configure(EntityTypeBuilder<EventUserRoleRecord> builder)
    {
        builder.ToTable("event_user_roles");

        builder.HasKey(eventUserRole => new { eventUserRole.EventId, eventUserRole.UserId });

        builder.Property(eventUserRole => eventUserRole.EventId).HasColumnName("event_id");
        builder.Property(eventUserRole => eventUserRole.UserId).HasColumnName("user_id");
        builder.Property(eventUserRole => eventUserRole.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(eventUserRole => eventUserRole.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(eventUserRole => eventUserRole.UserId).HasDatabaseName("ix_event_user_roles_user_id");

        builder.HasOne(eventUserRole => eventUserRole.User)
            .WithMany()
            .HasForeignKey(eventUserRole => eventUserRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
