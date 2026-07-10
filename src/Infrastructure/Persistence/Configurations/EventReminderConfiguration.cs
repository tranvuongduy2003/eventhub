using EventHub.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal sealed class EventReminderConfiguration : IEntityTypeConfiguration<EventReminderRecord>
{
    public void Configure(EntityTypeBuilder<EventReminderRecord> builder)
    {
        builder.ToTable("event_reminders");

        builder.HasKey(reminder => reminder.EventId);

        builder.Property(reminder => reminder.EventId)
            .HasColumnName("event_id")
            .ValueGeneratedNever();

        builder.Property(reminder => reminder.Enabled)
            .HasColumnName("enabled")
            .IsRequired();

        builder.Property(reminder => reminder.LeadTimeMinutes)
            .HasColumnName("lead_time_minutes")
            .IsRequired();

        builder.Property(reminder => reminder.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(reminder => reminder.LastSentAt)
            .HasColumnName("last_sent_at");

        builder.HasOne<EventRecord>()
            .WithMany()
            .HasForeignKey(reminder => reminder.EventId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_event_reminders_events_event_id");
    }
}
