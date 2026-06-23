using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Events;

public sealed class Occurrence : Entity<OccurrenceId>
{
    private Occurrence()
    {
    }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public string? VenueName { get; private set; }

    public string? Address { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Occurrence Schedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset createdAt)
    {
        if (endsAt <= startsAt)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_ENDS_BEFORE_START",
                "Occurrence end time must be after start time.");
        }

        return new Occurrence
        {
            StartsAt = startsAt,
            EndsAt = endsAt,
            VenueName = venueName,
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    internal void Reschedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset updatedAt)
    {
        if (endsAt <= startsAt)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_ENDS_BEFORE_START",
                "Occurrence end time must be after start time.");
        }

        StartsAt = startsAt;
        EndsAt = endsAt;
        VenueName = venueName;
        Address = address;
        UpdatedAt = updatedAt;
    }

    public static Occurrence FromPersistence(
        OccurrenceId id,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new()
        {
            Id = id,
            StartsAt = startsAt,
            EndsAt = endsAt,
            VenueName = venueName,
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
}
